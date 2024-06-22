using NobunAtelier;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    public class Instance : DataInstance<ModularAbilityDefinition>, IAbilityInstance, ContextualLogManager.IStateProvider
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
                m_previousState = m_currentState;
                m_currentState = value;
            }
        }

        private IAbilityInstance.ExecutionState m_previousState = IAbilityInstance.ExecutionState.Ready;
        private IAbilityInstance.ExecutionState m_currentState = IAbilityInstance.ExecutionState.Ready;

        // private AbilityController m_controller;
        private AbilityModuleController m_moduleController = null;
        private float m_lastChargeDuration = 0;
        private int m_currentChargeLevel = -1;

        private Dictionary<int, Command> m_commandLookupTable;
        private Queue<Command> m_commandQueue;
        private Command m_activeCommand;
        private CommandCategory m_nextCommandCategory = CommandCategory.Default;
        public bool IsCharging { get; private set; }
        private ContextualLogManager.LogPartition m_LogSection;

        public Instance(ModularAbilityDefinition data, AbilityController controller)
            : base(data)
        {
            LogPartitionName = $"'{Data.name}' Instance";
            m_LogSection = ContextualLogManager.Register(controller, Data.m_LogSettings, this);

            m_moduleController = new AbilityModuleController(controller);
            m_activeCommand = null;
            //m_controller = controller;

            m_commandLookupTable = new Dictionary<int, Command>()
            {
                { (int)CommandCategory.Default,  new Command(this, Data.m_Default) },
            };

            if (Data.m_canBeCharged)
            {
                m_commandLookupTable.Add((int)CommandCategory.ChargeStart, new Command(this, Data.m_ChargeStart));
                m_commandLookupTable.Add((int)CommandCategory.ChargeCancel, new Command(this, Data.m_ChargeCancel));

                int i = 0;
                foreach (var chargeData in Data.m_chargedAbilityLevels)
                {
                    m_commandLookupTable.Add((int)CommandCategory.ChargeLevelReached + i, new Command(this, chargeData.OnLevelReached));
                    m_commandLookupTable.Add((int)CommandCategory.ChargeExecution + i, new Command(this, chargeData.OnChargeReleased));
                    ++i;
                }
            }

            m_commandQueue = new Queue<Command>();
        }

        ~Instance()
        {
            ContextualLogManager.Unregister(m_LogSection);
        }

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
            if (m_activeCommand == null)
            {
                return;
            }

            m_LogSection.Record();

            m_activeCommand.Execute();
            m_nextCommandCategory = CommandCategory.Default;
        }

        public void UpdateEffect(float deltaTime)
        {
            if (ExecutionState != IAbilityInstance.ExecutionState.InProgress &&
                ExecutionState != IAbilityInstance.ExecutionState.Charging &&
                (m_activeCommand == null && m_commandQueue.Count == 0))
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
                        m_activeCommand.Update(deltaTime);
                        // m_stateMachine.Update(deltaTime);
                        break;
                    }

                case IAbilityInstance.ExecutionState.Charging:
                    {
                        if (m_commandQueue.Count > 0)
                        {
                            if (m_activeCommand != null)
                            {
                                m_activeCommand.Stop();
                            }

                            if (m_commandQueue.TryDequeue(out m_activeCommand))
                            {
                                m_activeCommand.Initiate();
                            }
                        }
                        else if (m_activeCommand != null)
                        {
                            // else update existing if any...
                            m_activeCommand.Update(deltaTime);
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

        private void ExecuteNewCommand(float deltaTime)
        {
            if (m_activeCommand != null)
            {
                // Should not be necessary, but just in case for now as
                // non processor can't know when to stop...
                m_activeCommand.Stop();
            }

            if (m_commandQueue.TryDequeue(out m_activeCommand))
            {
                m_activeCommand.Initiate();
                ExecutionState = IAbilityInstance.ExecutionState.Starting;
            }
        }

        public void StopEffect()
        {
            if (m_activeCommand == null)
            {
                return;
            }

            m_LogSection.Record();

            m_activeCommand.Stop();
            m_activeCommand = null;
            if (!Data.m_canChainOnSelf)
            {
                ExecutionState = IAbilityInstance.ExecutionState.Cooldown;
                return;
            }

            ExecutionState = IAbilityInstance.ExecutionState.ChainOpportunity;
            OnAbilityChainOpportunity?.Invoke();
        }

        public void TerminateExecution()
        {
            // Early exit in case we are not actually running an ability yet.
            if (ExecutionState == IAbilityInstance.ExecutionState.Ready)
            {
                return;
            }

            if (m_activeCommand != null)
            {
                m_LogSection.Record("StopEffect");
                m_activeCommand.Stop();
                m_activeCommand = null;
            }

            m_currentChargeLevel = -1;
            m_nextCommandCategory = CommandCategory.Default;
            ExecutionState = IAbilityInstance.ExecutionState.Ready;
            OnAbilityCompleteExecution?.Invoke();
            m_LogSection.Record("Execution terminated.");
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
            if (Data.m_canBeCharged && ExecutionState == IAbilityInstance.ExecutionState.InProgress)
            {
                m_LogSection.Record("Aborted because an ability is in progress...");
                return;
            }

            if (!Data.m_canBeCharged || (ExecutionState != IAbilityInstance.ExecutionState.Ready
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

                if (Data.m_cancelAbilityChargeOnEarlyChargeRelease)
                {
                    m_LogSection.Record("[1] - Canceling Ability Charge On Early Charge Release");
                    CancelCharge();
                }

                if (Data.m_playAbilityOnEarlyChargeRelease)
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

        private void UpdateAbilityChargeLevel()
        {
            if (!IsCharging)
            {
                return;
            }

            // Log("<b>UpdateAbilityChargeLevel</b>");

            switch (Data.m_chargeConstraint)
            {
                case ChargeReleaseConstraint.None:
                    break;

                case ChargeReleaseConstraint.ReleaseOnMaxChargeReached:
                    if (m_currentChargeLevel >= Data.m_chargedAbilityLevels.Length - 1)
                    {
                        m_LogSection.Record($"ReleaseOnMaxChargeReached");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case ChargeReleaseConstraint.ReleaseOnTimeout:
                    if (m_lastChargeDuration >= Data.m_chargeTimeout)
                    {
                        m_LogSection.Record($"ReleaseOnTimeout");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case ChargeReleaseConstraint.CancelOnTimeout:
                    if (m_lastChargeDuration >= Data.m_chargeTimeout)
                    {
                        m_LogSection.Record($"CancelOnTimeout");
                        CancelCharge();
                        return;
                    }
                    break;
            }

            int maxLevel = Data.m_chargedAbilityLevels.Length;

            // If we already reached the max level, exit now.
            if (m_currentChargeLevel >= maxLevel - 1)
            {
                return;
            }

            float cumulativeDuration = 0;
            for (int i = 0; i < maxLevel; i++)
            {
                ChargeLevelData level = Data.m_chargedAbilityLevels[i];
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
                m_moduleController.StopModules(m_modules);
            }
        }

        private class Command
        {
            private ActionModel m_abilityAction;
            private IReadOnlyList<AbilityModuleDefinition> m_modules;
            private AbilityModuleController m_moduleController;
            private Instance m_target;
            private IModularAbilityProcessor m_processor;

            public Command(Instance target, ActionModel abilityAction)
            {
                m_abilityAction = abilityAction;
                m_modules = abilityAction.Modules;
                m_target = target;
                m_moduleController = target.m_moduleController;
                m_moduleController.Add(abilityAction.Modules);

                foreach (var module in abilityAction.Modules)
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
                m_moduleController.InitiateModulesExecution(m_modules);

                if (!m_abilityAction.QuietExecution)
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
                    CoroutineManager.Start(AutoExecuteRoutine());
                }
            }

            public void Execute()
            {
                m_moduleController.ExecuteModules(m_modules);
            }

            public void Update(float deltaTime)
            {
                m_moduleController.UpdateModules(deltaTime, m_modules);
            }

            public void Stop()
            {
                m_moduleController.StopModules(m_modules);
            }

            private IEnumerator AutoExecuteRoutine()
            {
                yield return new WaitForSeconds(m_abilityAction.ExecutionDelay);
                Execute();
                yield return new WaitForSeconds(m_abilityAction.UpdateDuration);
                Stop();
                yield return new WaitForSeconds(m_abilityAction.ChainOpportunityDuration);

                if (!m_abilityAction.TerminateExecutionOnCompletion)
                {
                    yield break;
                }

                m_target.TerminateExecution();
            }
        }
    }
}
