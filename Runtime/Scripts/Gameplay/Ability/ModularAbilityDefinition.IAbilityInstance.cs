using NobunAtelier;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public partial class ModularAbilityDefinition
{
    [Header("Log")]
    [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

    public override IAbilityInstance CreateAbilityInstance(AbilityController controller)
    {
        return new Instance(this, controller);
    }

    [System.Flags]
    private enum DebugFlags
    {
        Info = 1 << 1,
        Update = 1 << 2,
        Warning = 1 << 3,
        Error = 1 << 4,
    }

    public class Instance : DataInstance<ModularAbilityDefinition>, IAbilityInstance, ContextualLogManager.IStateProvider, IDisposable
    {
        public event Action OnAbilityStartCharge;

        public event Action OnAbilityInitiated;

        public event Action OnAbilityChainOpportunity;

        public event Action OnAbilityCompleteExecution;

        public AbilityDefinition Ability => Data;
        public string LogPartitionName { get; private set; }

        public IAbilityInstance.ExecutionState ExecutionState
        {
            get => m_currentState;
            private set
            {
                m_currentState = value;
            }
        }

        private IAbilityInstance.ExecutionState m_currentState = IAbilityInstance.ExecutionState.Ready;

        // private AbilityController m_controller;
        private AbilityModuleController m_moduleController = null;
        private float m_lastChargeDuration = 0;
        private int m_currentChargeLevel = -1;

        private Dictionary<int, AbilityCommand> m_commandLookupTable;
        private Queue<AbilityCommand> m_commandQueue;
        private CommandRunner m_activeCommandRunner;
        private CommandCategory m_nextCommandCategory = CommandCategory.Default;
        public bool IsCharging { get; private set; }
        private ContextualLogManager.LogPartition m_LogSection;

        public Instance(ModularAbilityDefinition data, AbilityController controller)
            : base(data)
        {
            LogPartitionName = $"'{Data.name}' Instance";
            m_LogSection = ContextualLogManager.Register(controller, Data.m_LogSettings, this);

            m_moduleController = new AbilityModuleController(controller);
            m_activeCommandRunner = null;
            //m_controller = controller;

            m_commandLookupTable = new Dictionary<int, AbilityCommand>()
            {
                { (int)CommandCategory.Default,  new AbilityCommand(Data.m_Default) },
            };

            if (Data.m_CanBeCharged)
            {
                m_commandLookupTable.Add((int)CommandCategory.ChargeStart, new AbilityCommand(Data.m_ChargeStart));
                m_commandLookupTable.Add((int)CommandCategory.ChargeCancel, new AbilityCommand(Data.m_ChargeCancel));

                int i = 0;
                foreach (var chargeData in Data.m_ChargedAbilityLevels)
                {
                    m_commandLookupTable.Add((int)CommandCategory.ChargeLevelReached + i, new AbilityCommand(chargeData.OnLevelReached));
                    m_commandLookupTable.Add((int)CommandCategory.ChargeExecution + i, new AbilityCommand(chargeData.OnChargeReleased));
                    ++i;
                }
            }

            m_commandQueue = new Queue<AbilityCommand>();
        }

        private bool m_IsDisposed;

        /// <summary>
        /// AtelierLog.IStateProvider implementation.
        /// </summary>
        /// <returns></returns>
        public string GetStateMessage()
        {
            return $"{typeof(Instance).Name} of <b>{Data.name}</b>:" +
                $"\nState: {ExecutionState}; IsCharging: {IsCharging}; Command Queue Count: {m_commandQueue.Count};" +
                $"\nCurrentChargeLevel: {m_currentChargeLevel}; LastChargeDuration: {m_lastChargeDuration}s";
        }

        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_IsDisposed = true;
            if (ExecutionState != IAbilityInstance.ExecutionState.Ready)
            {
                TerminateExecution(false);
            }
            m_commandQueue.Clear();
            IsCharging = false;
            ContextualLogManager.Unregister(m_LogSection);
        }

        public bool CanExecute()
        {
            return ExecutionState == IAbilityInstance.ExecutionState.Ready
                || ExecutionState == IAbilityInstance.ExecutionState.ChainOpportunity;
        }

        public void InitiateExecution()
        {
            m_commandQueue.Enqueue(m_commandLookupTable[(int)m_nextCommandCategory]);
        }

        public void ExecuteEffect()
        {
            if (m_activeCommandRunner == null)
            {
                return;
            }

            m_LogSection.Record();

            m_activeCommandRunner.Execute();
            m_nextCommandCategory = CommandCategory.Default;
        }

        public void UpdateEffect(float deltaTime)
        {
            if (ExecutionState != IAbilityInstance.ExecutionState.InProgress &&
                ExecutionState != IAbilityInstance.ExecutionState.Charging &&
                (m_activeCommandRunner == null && m_commandQueue.Count == 0))
            {
                return;
            }

            m_LogSection.Record(ContextualLogManager.LogTypeFilter.Update);

            switch (ExecutionState)
            {
                case IAbilityInstance.ExecutionState.Ready:
                case IAbilityInstance.ExecutionState.ChainOpportunity:
                    ExecuteNewCommand(deltaTime);
                    break;

                // Transitive state before in progress to prevent new command execution.
                case IAbilityInstance.ExecutionState.Starting:
                    ExecutionState = IAbilityInstance.ExecutionState.InProgress;
                    break;

                case IAbilityInstance.ExecutionState.InProgress:
                    {
                        m_activeCommandRunner.Update(deltaTime);
                        // m_stateMachine.Update(deltaTime);
                        break;
                    }

                case IAbilityInstance.ExecutionState.Charging:
                    {
                        if (m_commandQueue.Count > 0)
                        {
                            if (m_activeCommandRunner != null)
                            {
                                m_activeCommandRunner.Stop(true);
                            }

                            if (m_commandQueue.TryDequeue(out var command))
                            {
                                ActivateCommand(command);
                            }
                        }
                        else if (m_activeCommandRunner != null)
                        {
                            // else update existing if any...
                            m_activeCommandRunner.Update(deltaTime);
                        }

                        // We need to ensure that the first frame after a charge release,
                        // we don't let the ability continue to charge...
                        if (m_currentChargeLevel > 0)
                        {
                            IsCharging = false;
                            return;
                        }

                        m_lastChargeDuration += deltaTime;
                        UpdateAbilityChargeLevel();
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Stop active command (modules effect) and change the state to Cooldown or ChainOpportunity.
        /// </summary>
        /// <param name="backgroundExecution">If set to true, only stop the active command and
        /// prevents the change of state (Cooldown or ChainOpportunity).</param>
        public void StopEffect()
        {
            StopEffect(false);
        }

        private void StopEffect(bool backgroundExecution)
        {
            if (m_activeCommandRunner == null)
            {
                return;
            }

            m_LogSection.Record();

            m_activeCommandRunner.Stop(false);
            m_activeCommandRunner = null;

            if (backgroundExecution)
            {
                return;
            }

            if (!Data.m_CanChainOnSelf)
            {
                ExecutionState = IAbilityInstance.ExecutionState.Cooldown;
                return;
            }

            ExecutionState = IAbilityInstance.ExecutionState.ChainOpportunity;
            OnAbilityChainOpportunity?.Invoke();
        }

        /// <summary>
        /// Terminate the active command (module effects) and reset the ability instance.
        /// </summary>
        /// <param name="raiseEvent">If enabled, raise OnAbilityCompleteExecution event.</param>
        public void TerminateExecution()
        {
            TerminateExecution(true);
        }

        private void TerminateExecution(bool raiseEvent)
        {
            // Early exit in case we are not actually running an ability yet.
            if (ExecutionState == IAbilityInstance.ExecutionState.Ready)
            {
                m_LogSection.Record("Early exit because no ability is running.", ContextualLogManager.LogTypeFilter.Warning);
                return;
            }

            m_LogSection.Record();

            if (m_activeCommandRunner != null)
            {
                m_LogSection.Record("StopEffect");
                m_activeCommandRunner.Stop(true);
                m_activeCommandRunner = null;
            }

            m_currentChargeLevel = -1;
            m_nextCommandCategory = CommandCategory.Default;
            ExecutionState = IAbilityInstance.ExecutionState.Ready;

            if (raiseEvent)
            {
                OnAbilityCompleteExecution?.Invoke();
            }
        }

        public void StartCharge()
        {
            m_LogSection.Record();

            // Added InProgress to allows easier smoother chains - otherwise calling StartCharge
            // while an ability is InProgress would result in queuing another default...
            // But if we add that condition to the above, it is possible to cancel in progress ability
            // with a new start charge when we only want to cancel it when chain opportunity.
            // However the issue is that we would like a way to buffer the player intention...
            // Like if he hold the button after an attack, should we wait for ChainOpportunity,
            // and then StartCharge?
            // This would be a lot of work, but would allow to achieve MHW like result.
            // Tbf, after a few test it feel solid, just like a fighting game, where if you
            // input too early, nothing happen, but right in time and you can chain smoothly
            if (Data.m_CanBeCharged && ExecutionState == IAbilityInstance.ExecutionState.InProgress)
            {
                m_LogSection.Record("Aborted because an ability is in progress...");
                return;
            }

            if (!Data.m_CanBeCharged || (ExecutionState != IAbilityInstance.ExecutionState.Ready
                    && ExecutionState != IAbilityInstance.ExecutionState.ChainOpportunity))
            {
                m_LogSection.Record($"Ability can't be charged. Playing normal ability instead.", ContextualLogManager.LogTypeFilter.Warning);
                m_nextCommandCategory = CommandCategory.Default;
                InitiateExecution();
                return;
            }


            m_commandQueue.Enqueue(m_commandLookupTable[(int)CommandCategory.ChargeStart]);

            IsCharging = true;
            ExecutionState = IAbilityInstance.ExecutionState.Charging;
            m_currentChargeLevel = -1;
            m_lastChargeDuration = 0;

            OnAbilityStartCharge?.Invoke();
        }

        public void ReleaseCharge()
        {
            if (!IsCharging)
            {
                m_LogSection.Record("Ignored - Not charging.");
                return;
            }

            m_LogSection.Record();

            // If charge started but haven't reached a level yet.
            if (m_currentChargeLevel < 0)
            {
                // In case the ability was not charged enough, we can PlayAbility instead.
                // we don't need to bother about module effect has none has started yet.

                if (Data.m_CancelAbilityChargeOnEarlyChargeRelease)
                {
                    m_LogSection.Record("[1] - Canceling Ability Charge On Early Charge Release");
                    CancelCharge();
                }

                if (Data.m_PlayAbilityOnEarlyChargeRelease)
                {
                    // TODO: This is for ability chain
                    // m_hasAlreadyBeenChainFromStartCharge = State == IAbilityInstance.ExecutionState.ChainOpportunity;

                    m_LogSection.Record("[2] - Playing Ability On Early Charge Release");
                    ExecutionState = IAbilityInstance.ExecutionState.Ready;
                    m_nextCommandCategory = CommandCategory.Default;
                    InitiateExecution();
                }

                return;
            }

            // Otherwise queue new action model.
            m_nextCommandCategory = (CommandCategory)((int)CommandCategory.ChargeExecution + m_currentChargeLevel);
            ExecutionState = IAbilityInstance.ExecutionState.Ready;
            InitiateExecution();
            IsCharging = false;
        }

        public void CancelCharge()
        {
            if (!IsCharging)
            {
                m_LogSection.Record("Failed to CancelCharge");
                return;
            }

            m_LogSection.Record();

            m_commandQueue.Enqueue(m_commandLookupTable[(int)CommandCategory.ChargeCancel]);

            m_currentChargeLevel = -1;
            ExecutionState = IAbilityInstance.ExecutionState.Ready;
            IsCharging = false;
        }

        private void ExecuteNewCommand(float deltaTime)
        {
            if (m_activeCommandRunner != null)
            {
                // Should not be necessary, but just in case for now as
                // non processor can't know when to stop...
                m_activeCommandRunner.Stop(true);
            }

            if (m_commandQueue.TryDequeue(out var command))
            {
                ActivateCommand(command);
                ExecutionState = IAbilityInstance.ExecutionState.Starting;
            }
        }

        private void ActivateCommand(AbilityCommand command)
        {
            m_activeCommandRunner = new CommandRunner(this, command);
            m_activeCommandRunner.Initiate();
        }


        private void UpdateAbilityChargeLevel()
        {
            if (!IsCharging)
            {
                return;
            }

            // Log("<b>UpdateAbilityChargeLevel</b>");

            switch (Data.m_ChargeConstraint)
            {
                case ChargeReleaseConstraint.None:
                    break;

                case ChargeReleaseConstraint.ReleaseOnMaxChargeReached:
                    if (m_currentChargeLevel >= Data.m_ChargedAbilityLevels.Length - 1)
                    {
                        m_LogSection.Record($"ReleaseOnMaxChargeReached");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case ChargeReleaseConstraint.ReleaseOnTimeout:
                    if (m_lastChargeDuration >= Data.m_ChargeTimeout)
                    {
                        m_LogSection.Record($"ReleaseOnTimeout");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case ChargeReleaseConstraint.CancelOnTimeout:
                    if (m_lastChargeDuration >= Data.m_ChargeTimeout)
                    {
                        m_LogSection.Record($"CancelOnTimeout");
                        CancelCharge();
                        return;
                    }
                    break;
            }

            int maxLevel = Data.m_ChargedAbilityLevels.Length;

            // If we already reached the max level, exit now.
            if (m_currentChargeLevel >= maxLevel - 1)
            {
                return;
            }

            float cumulativeDuration = 0;
            for (int i = 0; i < maxLevel; i++)
            {
                ChargeLevelData level = Data.m_ChargedAbilityLevels[i];
                cumulativeDuration += level.TresholdDuration;

                // Reach level duration treshold.
                if (m_lastChargeDuration >= cumulativeDuration)
                {
                    // If we already reached this level. The level effect already been activated.
                    if (m_currentChargeLevel >= i)
                    {
                        break;
                    }

                    // Level reached for the first time.
                    m_currentChargeLevel = i;
                    m_commandQueue.Enqueue(m_commandLookupTable[(int)CommandCategory.ChargeLevelReached + i]);
                    m_LogSection.Record($"<b>On Charge Level '{i}' reached</b>");
                }
            }
        }

        //private void Log(string message = "", DebugFlags flags = DebugFlags.Info, [CallerMemberName] string funcName = null)
        //{
        //    if ((Data.m_debug & flags) == 0)
        //    {
        //        return;
        //    }

        //    message = $"[{Time.frameCount}] <b>{Data.name}</b><{funcName}()> {message}" +
        //        $"\nState: {ExecutionState}; IsCharging: {IsCharging}; Command Queue Count: {m_commandQueue.Count};" +
        //        $"\nCurrentChargeLevel: {m_currentChargeLevel}; LastChargeDuration: {m_lastChargeDuration}s" + // m_chainIndex: {m_chainIndex}" +
        //        $"\n";

        //    if ((flags & DebugFlags.Warning) != 0)
        //    {
        //        Debug.LogWarning(message, Data);
        //    }
        //    else
        //    {
        //        Debug.Log(message, Data);
        //    }
        //}

        private enum CommandCategory
        {
            Default = 0,
            ChargeStart = 1,
            ChargeCancel = 2,

            ChargeLevelReached = 1 << 8,
            ChargeExecution = 1 << 16,
        }

        private class ModuleStateMachine
        {
            private IReadOnlyDictionary<int, ModuleState> m_states;
            private ModuleState m_currentState;

            public ModuleStateMachine(IReadOnlyDictionary<int, ModuleState> modularAbilityStates)
            {
                m_states = modularAbilityStates;
            }

            public void Initiate(int state)
            {
                if (m_currentState != null)
                {
                    m_currentState.ExitState();
                }

                if (m_states.TryGetValue(state, out m_currentState))
                {
                    m_currentState.ResetState();
                }
            }

            public void Execute()
            {
                if (m_currentState == null)
                {
                    return;
                }

                m_currentState.EnterState();
            }

            public void Update(float deltaTime)
            {
                if (m_currentState == null)
                {
                    return;
                }

                m_currentState.UpdateState(deltaTime);
            }

            public void Stop()
            {
                if (m_currentState == null)
                {
                    return;
                }

                m_currentState.ExitState();
            }
        }

        private class ModuleState
        {
            private IReadOnlyList<AbilityModuleDefinition> m_modules;
            private AbilityModuleController m_moduleController;

            public ModuleState(IReadOnlyList<AbilityModuleDefinition> modules, AbilityModuleController moduleController)
            {
                m_modules = modules;
                m_moduleController = moduleController;
                m_moduleController.Add(modules);
            }

            public void ResetState()
            {
                m_moduleController.InitiateModulesExecution(m_modules);
            }

            public void EnterState()
            {
                m_moduleController.ExecuteModules(m_modules);
            }

            public void UpdateState(float deltaTime)
            {
                m_moduleController.UpdateModules(deltaTime, m_modules);
            }

            public void ExitState()
            {
            m_moduleController.StopModules(m_modules, true);
            }
        }

        private readonly struct AbilityCommand
        {
            public readonly ActionModel ActionModel;

            public AbilityCommand(ActionModel actionModel)
            {
                ActionModel = actionModel;
            }
        }

        private class CommandRunner
        {
            private ActionModel m_abilityAction;
            private IReadOnlyList<AbilityModuleDefinition> m_modules;
            private AbilityModuleController m_moduleController;
            private Instance m_target;
            private IModularAbilityProcessor m_processor;
            private CancellationTokenSource m_CancellationTokenSource;

            public CommandRunner(Instance target, AbilityCommand command)
            {
                m_abilityAction = command.ActionModel;
                m_modules = m_abilityAction != null ? m_abilityAction.Modules : Array.Empty<AbilityModuleDefinition>();
                m_target = target;
                m_moduleController = target.m_moduleController;
                if (m_modules.Count > 0)
                {
                    m_moduleController.Add(m_modules);
                }

                foreach (var module in m_modules)
                {
                    if (m_moduleController.m_modulesMap.TryGetValue(module, out var instance))
                    {
                        if (instance is IModularAbilityProcessor)
                        {
                            m_processor = instance as IModularAbilityProcessor;
                            break;
                        }
                    }
                }
            }

            public void Initiate()
            {
                if (m_abilityAction == null)
                {
                    m_target.m_LogSection.Record("Missing ActionModel for command.", ContextualLogManager.LogTypeFilter.Warning);
                    return;
                }

                m_moduleController.InitiateModulesExecution(m_modules);

                if (!m_abilityAction.BackgroundExecution)
                {
                    m_target.OnAbilityInitiated?.Invoke();
                }

                m_target.m_LogSection.Record("Initiate Ability");

                if (m_processor != null)
                {
                    m_processor.RequestExecution();
                }
                else
                {
                    RestartAutoExecute();
                }
            }

            public void Execute()
            {
                if (m_abilityAction == null)
                {
                    return;
                }

                m_moduleController.ExecuteModules(m_modules);
            }

            public void Update(float deltaTime)
            {
                if (m_abilityAction == null)
                {
                    return;
                }

                m_moduleController.UpdateModules(deltaTime, m_modules);
            }

            public void Stop(bool includeProcessors)
            {
                CancelAutoExecute();
                m_moduleController.StopModules(m_modules, includeProcessors);
            }

            private void RestartAutoExecute()
            {
                CancelAutoExecute();
                m_CancellationTokenSource = new CancellationTokenSource();
                AutoExecuteAsync(m_CancellationTokenSource.Token).FireAndForget();
            }

            private void CancelAutoExecute()
            {
                if (m_CancellationTokenSource == null)
                {
                    return;
                }

                m_CancellationTokenSource.Cancel();
                m_CancellationTokenSource.Dispose();
                m_CancellationTokenSource = null;
            }

            private async Awaitable AutoExecuteAsync(CancellationToken cancellationToken)
            {
                if (m_abilityAction.ExecutionDelay > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_abilityAction.ExecutionDelay, cancellationToken);
                }

                m_target.ExecuteEffect();

                if (m_abilityAction.UpdateDuration > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_abilityAction.UpdateDuration, cancellationToken);
                }

                m_target.StopEffect(m_abilityAction.BackgroundExecution);

                if (m_abilityAction.ChainOpportunityDuration > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_abilityAction.ChainOpportunityDuration, cancellationToken);
                }

                if (!m_abilityAction.TerminateExecutionOnCompletion)
                {
                    return;
                }

                m_target.TerminateExecution();
            }
        }
    }
}
