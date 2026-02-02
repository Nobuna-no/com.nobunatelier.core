using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public partial class ModularAbilityDefinition
    {
        [Header("Log")]
        [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

        public override IAbilityInstance CreateAbilityInstance(AbilityController controller)
        {
            return new Instance(this, controller);
        }

        public class Instance : DataInstance<ModularAbilityDefinition>, IAbilityInstance, ContextualLogManager.IStateProvider, IDisposable
        {
            public event Action OnAbilityStartCharge;

            public event Action OnAbilityInitiated;

            internal void InvokeAbilityInitiated() => OnAbilityInitiated?.Invoke();

            public event Action OnAbilityChainOpportunity;

            public event Action OnAbilityCompleteExecution;

            public AbilityDefinition Ability => Data;
            public string LogPartitionName { get; private set; }

            public IAbilityInstance.ExecutionState ExecutionState => m_StateMachine.ExecutionState;

            internal AbilityModuleRegistry ModuleRegistry { get; private set; }
            private AbilityCommandQueue m_CommandQueue = null;
            private AbilityStateMachine m_StateMachine = null;
            public bool IsCharging => m_StateMachine.IsCharging;
            internal ContextualLogManager.LogPartition LogSection { get; private set; }

            public Instance(ModularAbilityDefinition data, AbilityController controller)
                : base(data)
            {
                LogPartitionName = $"'{Data.name}' Instance";
                LogSection = ContextualLogManager.Register(controller, Data.m_LogSettings, this);

                ModuleRegistry = new AbilityModuleRegistry(controller);
                m_CommandQueue = new AbilityCommandQueue(this, ModuleRegistry, LogSection);
                m_StateMachine = new AbilityStateMachine(Data, m_CommandQueue, LogSection,
                    () => OnAbilityStartCharge?.Invoke(),
                    () => OnAbilityChainOpportunity?.Invoke(),
                    () => OnAbilityCompleteExecution?.Invoke());
            }

            private bool m_IsDisposed;

            /// <summary>
            /// AtelierLog.IStateProvider implementation.
            /// </summary>
            /// <returns></returns>
            public string GetStateMessage()
            {
                return $"{typeof(Instance).Name} of <b>{Data.name}</b>:" +
                    $"\nState: {ExecutionState}; IsCharging: {IsCharging}; Command Queue Count: {m_CommandQueue.CommandCount};" +
                    $"\nCurrentChargeLevel: {m_StateMachine.CurrentChargeLevel}; LastChargeDuration: {m_StateMachine.LastChargeDuration}s";
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
                m_CommandQueue.Clear();
                m_StateMachine.ResetChargeState();
                ContextualLogManager.Unregister(LogSection);
            }

            public bool CanExecute()
            {
                return m_StateMachine.CanExecute();
            }

            public void InitiateExecution()
            {
                m_StateMachine.InitiateExecution();
            }

            public void ExecuteEffect()
            {
                m_StateMachine.ExecuteEffect();
            }

            public void UpdateEffect(float deltaTime)
            {
                m_StateMachine.UpdateEffect(deltaTime);
            }

            /// <summary>
            /// Stop active command (modules effect) and change the state to Cooldown or ChainOpportunity.
            /// </summary>
            /// <param name="backgroundExecution">If set to true, only stop the active command and
            /// prevents the change of state (Cooldown or ChainOpportunity).</param>
            public void StopEffect(bool backgroundExecution = false)
            {
                m_StateMachine.StopEffect(backgroundExecution);
            }

            /// <summary>
            /// Terminate the active command (module effects) and reset the ability instance.
            /// </summary>
            /// <param name="raiseEvent">If enabled, raise OnAbilityCompleteExecution event.</param>
            public void TerminateExecution()
            {
                m_StateMachine.TerminateExecution(true);
            }

            public void CancelExecution()
            {
                m_StateMachine.CancelExecution();
            }

            private void TerminateExecution(bool raiseEvent)
            {
                m_StateMachine.TerminateExecution(raiseEvent);
            }

            public void StartCharge()
            {
                m_StateMachine.StartCharge();
            }

            public void ReleaseCharge()
            {
                m_StateMachine.ReleaseCharge();
            }

            public void CancelCharge()
            {
                m_StateMachine.CancelCharge();
            }
        }

        internal enum CommandCategory
        {
            Default = 0,
            ChargeStart = 1,
            ChargeCancel = 2,

            ChargeLevelReached = 1 << 8,
            ChargeExecution = 1 << 16,
        }

        internal readonly struct AbilityCommand
        {
            public readonly ActionModel ActionModel;
            public readonly AbilityModuleDefinition TimingDriverModule;

            public AbilityCommand(ActionModel actionModel)
            {
                ActionModel = actionModel;
                TimingDriverModule = actionModel.ExecutionDriverModule;
            }
        }

        internal sealed class AbilityCommandQueue
        {
            private readonly Instance m_Owner;
            private readonly ModularAbilityDefinition m_Definition;
            private readonly Dictionary<int, AbilityCommand> m_CommandLookupTable;
            private readonly Queue<AbilityCommand> m_CommandQueue;
            private CommandRunner m_ActiveCommandRunner;

            public AbilityCommandQueue(Instance owner, AbilityModuleRegistry moduleController, ContextualLogManager.LogPartition logSection)
            {
                m_Owner = owner;
                m_Definition = owner.Data;
                m_CommandLookupTable = new Dictionary<int, AbilityCommand>()
                {
                    { (int)CommandCategory.Default, new AbilityCommand(m_Definition.m_Default) },
                };
            
                if (m_Definition.m_CanBeCharged)
                {
                    m_CommandLookupTable.Add((int)CommandCategory.ChargeStart, new AbilityCommand(m_Definition.m_ChargeStart));
                    m_CommandLookupTable.Add((int)CommandCategory.ChargeCancel, new AbilityCommand(m_Definition.m_ChargeCancel));

                    int i = 0;
                    foreach (var chargeData in m_Definition.m_ChargedAbilityLevels)
                    {
                        m_CommandLookupTable.Add((int)CommandCategory.ChargeLevelReached + i, new AbilityCommand(chargeData.OnLevelReached));
                        m_CommandLookupTable.Add((int)CommandCategory.ChargeExecution + i, new AbilityCommand(chargeData.OnChargeReleased));
                        ++i;
                    }
                }

                m_CommandQueue = new Queue<AbilityCommand>();
            }

            public int CommandCount => m_CommandQueue.Count;
            public bool HasActiveRunner => m_ActiveCommandRunner != null;
            public bool HasQueuedCommands => m_CommandQueue.Count > 0;

            public AbilityCommand GetCommand(CommandCategory category)
            {
                return m_CommandLookupTable[(int)category];
            }

            public void Enqueue(CommandCategory category)
            {
                m_CommandQueue.Enqueue(GetCommand(category));
            }

            public bool TryDequeue(out AbilityCommand command)
            {
                if (m_CommandQueue.Count == 0)
                {
                    command = default;
                    return false;
                }

                command = m_CommandQueue.Dequeue();
                return true;
            }

            public void ActivateCommand(AbilityCommand command)
            {
                m_ActiveCommandRunner = new CommandRunner(m_Owner, command);
                m_ActiveCommandRunner.Initiate();
            }

            public void ExecuteActive()
            {
                m_ActiveCommandRunner?.Execute();
            }

            public void UpdateActive(float deltaTime)
            {
                m_ActiveCommandRunner?.Update(deltaTime);
            }

            public void StopActive()
            {
                if (m_ActiveCommandRunner == null)
                {
                    return;
                }

                m_ActiveCommandRunner.Stop();
                m_ActiveCommandRunner = null;
            }

            public void CancelActive()
            {
                if (m_ActiveCommandRunner == null)
                {
                    return;
                }

                m_ActiveCommandRunner.Cancel();
                m_ActiveCommandRunner = null;
            }

            public void TerminateActive()
            {
                if (m_ActiveCommandRunner == null)
                {
                    return;
                }

                m_ActiveCommandRunner.Terminate();
                m_ActiveCommandRunner = null;
            }

            public void ClearQueuedCommands()
            {
                m_CommandQueue.Clear();
            }

            public void Clear()
            {
                m_CommandQueue.Clear();
                m_ActiveCommandRunner = null;
            }

            private sealed class CommandRunner
            {
                private ActionModel m_AbilityAction;
                private IReadOnlyList<AbilityModuleDefinition> m_Modules;
                private AbilityModuleRegistry m_ModuleRegistry;
                private Instance m_AbilityInstanceTarget;
                private IAbilityExecutionDriver m_ExecutionDriver;
                private AbilityExecutionDriverCallbacks m_TimingCallbacks;
                private AwaitableExecutionDriver m_AwaitableExecutionDriver;

                public CommandRunner(Instance target, AbilityCommand command)
                {
                    m_AbilityAction = command.ActionModel;
                    m_AbilityInstanceTarget = target;
                    m_ModuleRegistry = target.ModuleRegistry;
                    m_Modules = command.ActionModel.Modules;
                    if (m_Modules.Count > 0)
                    {
                        m_ModuleRegistry.Add(m_Modules);
                    }

                    if (command.TimingDriverModule != null)
                    {
                        if (m_ModuleRegistry.m_ModulesMap.TryGetValue(command.TimingDriverModule, out var instance))
                        {
                            m_ExecutionDriver = instance as IAbilityExecutionDriver;
                            if (m_ExecutionDriver == null)
                            {
                                m_AbilityInstanceTarget.LogSection.Record("Timing driver module instance does not implement IAbilityExecutionDriver. Falling back to Awaitable timing.",
                                    ContextualLogManager.LogTypeFilter.Warning);
                            }
                        }
                        else
                        {
                            m_AbilityInstanceTarget.LogSection.Record("Timing driver module instance not found. Falling back to Awaitable timing.",
                                ContextualLogManager.LogTypeFilter.Warning);
                        }
                    }

                    if (m_ExecutionDriver == null)
                    {
                        m_AwaitableExecutionDriver = new AwaitableExecutionDriver();
                        if (m_AbilityAction != null)
                        {
                            m_AwaitableExecutionDriver.ConfigureFromActionModel(m_AbilityAction);
                        }

                        m_ExecutionDriver = m_AwaitableExecutionDriver;
                    }
                }

                public void Initiate()
                {
                    if (m_AbilityAction == null)
                    {
                        m_AbilityInstanceTarget.LogSection.Record("Missing ActionModel for command.", ContextualLogManager.LogTypeFilter.Warning);
                        return;
                    }

                    if (m_ExecutionDriver == null)
                    {
                        m_AbilityInstanceTarget.LogSection.Record("Missing execution driver for command.", ContextualLogManager.LogTypeFilter.Warning);
                        return;
                    }

                    EnsureTimingCallbacks();
                    if (m_AwaitableExecutionDriver != null)
                    {
                        m_AwaitableExecutionDriver.ConfigureFromActionModel(m_AbilityAction);
                    }

                    m_ExecutionDriver.Initialize(new AbilityExecutionDriverContext(m_TimingCallbacks));

                    m_ModuleRegistry.InitiateModulesExecution(m_Modules);

                    if (!m_AbilityAction.BackgroundExecution)
                    {
                        m_AbilityInstanceTarget.InvokeAbilityInitiated();
                    }

                    m_AbilityInstanceTarget.LogSection.Record("Initiate Ability");

                    m_ExecutionDriver.RequestExecution();
                }

                public void Execute()
                {
                    if (m_AbilityAction == null)
                    {
                        return;
                    }

                    m_ModuleRegistry.ExecuteModules(m_Modules);
                }

                public void Update(float deltaTime)
                {
                    if (m_AbilityAction == null)
                    {
                        return;
                    }

                    m_ModuleRegistry.UpdateModules(deltaTime, m_Modules);
                }

                public void Stop()
                {
                    // Stop effect modules but keep timing driver alive
                    // Timing driver controls the lifecycle and needs to fire completion events
                    m_ModuleRegistry.StopModules(m_Modules);
                }

                public void Cancel()
                {
                    m_ModuleRegistry.StopModules(m_Modules);
                    m_ExecutionDriver?.Cancel();
                }

                public void Terminate()
                {
                    // Reset timing driver and stop all modules
                    m_ExecutionDriver?.Reset();
                    // m_moduleController.StopModules(m_modules);
                }

                private void EnsureTimingCallbacks()
                {
                    if (m_TimingCallbacks != null)
                    {
                        return;
                    }

                    m_TimingCallbacks = new AbilityExecutionDriverCallbacks(m_AbilityInstanceTarget, m_AbilityAction);
                }
            }
        }

        internal sealed class AbilityStateMachine
        {
            private readonly ModularAbilityDefinition m_Definition;
            private readonly AbilityCommandQueue m_CommandQueue;
            private readonly ContextualLogManager.LogPartition m_LogSection;
            private readonly Action m_OnAbilityStartCharge;
            private readonly Action m_OnAbilityChainOpportunity;
            private readonly Action m_OnAbilityCompleteExecution;
            private IAbilityInstance.ExecutionState m_CurrentState = IAbilityInstance.ExecutionState.Ready;
            private float m_LastChargeDuration = 0;
            private int m_CurrentChargeLevel = -1;
            private CommandCategory m_NextCommandCategory = CommandCategory.Default;

            public AbilityStateMachine(ModularAbilityDefinition definition, AbilityCommandQueue commandQueue, ContextualLogManager.LogPartition logSection,
                Action onAbilityStartCharge, Action onAbilityChainOpportunity, Action onAbilityCompleteExecution)
            {
                m_Definition = definition;
                m_CommandQueue = commandQueue;
                m_LogSection = logSection;
                m_OnAbilityStartCharge = onAbilityStartCharge;
                m_OnAbilityChainOpportunity = onAbilityChainOpportunity;
                m_OnAbilityCompleteExecution = onAbilityCompleteExecution;
            }

            public IAbilityInstance.ExecutionState ExecutionState => m_CurrentState;
            public bool IsCharging { get; private set; }
            public int CurrentChargeLevel => m_CurrentChargeLevel;
            public float LastChargeDuration => m_LastChargeDuration;

            public void ResetChargeState()
            {
                IsCharging = false;
                m_CurrentChargeLevel = -1;
                m_LastChargeDuration = 0;
            }

            public bool CanExecute()
            {
                return m_CurrentState == IAbilityInstance.ExecutionState.Ready
                    || m_CurrentState == IAbilityInstance.ExecutionState.ChainOpportunity;
            }

            public void InitiateExecution()
            {
                m_CommandQueue.Enqueue(m_NextCommandCategory);
            }

            public void ExecuteEffect()
            {
                if (!m_CommandQueue.HasActiveRunner)
                {
                    return;
                }

                m_LogSection.Record();

                m_CommandQueue.ExecuteActive();
                m_NextCommandCategory = CommandCategory.Default;
            }

            public void UpdateEffect(float deltaTime)
            {
                if (m_CurrentState != IAbilityInstance.ExecutionState.InProgress &&
                    m_CurrentState != IAbilityInstance.ExecutionState.Charging &&
                    (!m_CommandQueue.HasActiveRunner && !m_CommandQueue.HasQueuedCommands))
                {
                    return;
                }

                m_LogSection.Record(ContextualLogManager.LogTypeFilter.Update);

                switch (m_CurrentState)
                {
                    case IAbilityInstance.ExecutionState.Ready:
                    case IAbilityInstance.ExecutionState.ChainOpportunity:
                        ExecuteNewCommand();
                        break;

                    // Transitive state before in progress to prevent new command execution.
                    case IAbilityInstance.ExecutionState.Starting:
                        m_CurrentState = IAbilityInstance.ExecutionState.InProgress;
                        break;

                    case IAbilityInstance.ExecutionState.InProgress:
                        {
                            m_CommandQueue.UpdateActive(deltaTime);
                            break;
                        }

                    case IAbilityInstance.ExecutionState.Charging:
                        {
                            if (m_CommandQueue.HasQueuedCommands)
                            {
                                if (m_CommandQueue.HasActiveRunner)
                                {
                                    // Terminate current command to start charge command
                                    m_CommandQueue.TerminateActive();
                                }

                                if (m_CommandQueue.TryDequeue(out var command))
                                {
                                    ActivateCommand(command);
                                }
                            }
                            else if (m_CommandQueue.HasActiveRunner)
                            {
                                // else update existing if any...
                                m_CommandQueue.UpdateActive(deltaTime);
                            }

                            // We need to ensure that the first frame after a charge release,
                            // we don't let the ability continue to charge...
                            if (m_CurrentChargeLevel > 0)
                            {
                                IsCharging = false;
                                return;
                            }

                            m_LastChargeDuration += deltaTime;
                            UpdateAbilityChargeLevel();
                        }
                        break;

                    default:
                        break;
                }
            }

            public void StopEffect(bool backgroundExecution)
            {
                if (!m_CommandQueue.HasActiveRunner)
                {
                    return;
                }

                m_LogSection.Record();

                // Stop effect modules but keep timing driver alive to fire completion events
                m_CommandQueue.StopActive();

                if (backgroundExecution)
                {
                    return;
                }

                if (!m_Definition.m_CanChainOnSelf)
                {
                    m_CurrentState = IAbilityInstance.ExecutionState.Cooldown;
                    return;
                }

                m_CurrentState = IAbilityInstance.ExecutionState.ChainOpportunity;
                m_OnAbilityChainOpportunity?.Invoke();
            }

            public void CancelExecution()
            {
                if (m_CurrentState == IAbilityInstance.ExecutionState.Ready
                    && !m_CommandQueue.HasActiveRunner
                    && !m_CommandQueue.HasQueuedCommands)
                {
                    return;
                }

                m_LogSection.Record();

                if (m_CommandQueue.HasActiveRunner)
                {
                    m_CommandQueue.CancelActive();
                }

                m_CommandQueue.ClearQueuedCommands();
                ResetChargeState();
                m_NextCommandCategory = CommandCategory.Default;
                m_CurrentState = IAbilityInstance.ExecutionState.Ready;
            }

            public void TerminateExecution(bool raiseEvent)
            {
                // Early exit in case we are not actually running an ability yet.
                if (m_CurrentState == IAbilityInstance.ExecutionState.Ready)
                {
                    m_LogSection.Record("Early exit because no ability is running.", ContextualLogManager.LogTypeFilter.Warning);
                    return;
                }

                m_LogSection.Record();

                if (m_CommandQueue.HasActiveRunner)
                {
                    // m_LogSection.Record("TerminateExecution");
                    // Reset timing driver
                    m_CommandQueue.TerminateActive();
                }

                ResetChargeState();
                m_NextCommandCategory = CommandCategory.Default;
                m_CurrentState = IAbilityInstance.ExecutionState.Ready;

                if (raiseEvent)
                {
                    m_OnAbilityCompleteExecution?.Invoke();
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
                if (m_Definition.m_CanBeCharged && m_CurrentState == IAbilityInstance.ExecutionState.InProgress)
                {
                    m_LogSection.Record("Aborted because an ability is in progress...");
                    return;
                }

                if (!m_Definition.m_CanBeCharged || (m_CurrentState != IAbilityInstance.ExecutionState.Ready
                        && m_CurrentState != IAbilityInstance.ExecutionState.ChainOpportunity))
                {
                    m_LogSection.Record($"Ability can't be charged. Playing normal ability instead.", ContextualLogManager.LogTypeFilter.Warning);
                    m_NextCommandCategory = CommandCategory.Default;
                    InitiateExecution();
                    return;
                }

                m_CommandQueue.Enqueue(CommandCategory.ChargeStart);

                IsCharging = true;
                m_CurrentState = IAbilityInstance.ExecutionState.Charging;
                m_CurrentChargeLevel = -1;
                m_LastChargeDuration = 0;

                m_OnAbilityStartCharge?.Invoke();
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
                if (m_CurrentChargeLevel < 0)
                {
                    // In case the ability was not charged enough, we can PlayAbility instead.
                    // we don't need to bother about module effect has none has started yet.

                    if (m_Definition.m_CancelAbilityChargeOnEarlyChargeRelease)
                    {
                        m_LogSection.Record("[1] - Canceling Ability Charge On Early Charge Release");
                        CancelCharge();
                    }

                    if (m_Definition.m_PlayAbilityOnEarlyChargeRelease)
                    {
                        // TODO: This is for ability chain
                        // m_hasAlreadyBeenChainFromStartCharge = State == IAbilityInstance.ExecutionState.ChainOpportunity;

                        m_LogSection.Record("[2] - Playing Ability On Early Charge Release");
                        m_CurrentState = IAbilityInstance.ExecutionState.Ready;
                        m_NextCommandCategory = CommandCategory.Default;
                        InitiateExecution();
                    }

                    return;
                }

                // Otherwise queue new action model.
                m_NextCommandCategory = (CommandCategory)((int)CommandCategory.ChargeExecution + m_CurrentChargeLevel);
                m_CurrentState = IAbilityInstance.ExecutionState.Ready;
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

                m_CommandQueue.Enqueue(CommandCategory.ChargeCancel);

                m_CurrentChargeLevel = -1;
                m_CurrentState = IAbilityInstance.ExecutionState.Ready;
                IsCharging = false;
            }

            private void ExecuteNewCommand()
            {
                if (m_CommandQueue.HasActiveRunner)
                {
                    // Should not be necessary, but just in case for now as
                    // non processor can't know when to stop...
                    m_CommandQueue.TerminateActive();
                }

                if (m_CommandQueue.TryDequeue(out var command))
                {
                    ActivateCommand(command);
                    m_CurrentState = IAbilityInstance.ExecutionState.Starting;
                }
            }

            private void ActivateCommand(AbilityCommand command)
            {
                m_CommandQueue.ActivateCommand(command);
            }

            private void UpdateAbilityChargeLevel()
            {
                if (!IsCharging)
                {
                    return;
                }

                // Log("<b>UpdateAbilityChargeLevel</b>");

                switch (m_Definition.m_ChargeConstraint)
                {
                    case ChargeReleaseConstraint.None:
                        break;

                    case ChargeReleaseConstraint.ReleaseOnMaxChargeReached:
                        if (m_CurrentChargeLevel >= m_Definition.m_ChargedAbilityLevels.Length - 1)
                        {
                            m_LogSection.Record($"ReleaseOnMaxChargeReached");
                            ReleaseCharge();
                            return;
                        }
                        break;

                    case ChargeReleaseConstraint.ReleaseOnTimeout:
                        if (m_LastChargeDuration >= m_Definition.m_ChargeTimeout)
                        {
                            m_LogSection.Record($"ReleaseOnTimeout");
                            ReleaseCharge();
                            return;
                        }
                        break;

                    case ChargeReleaseConstraint.CancelOnTimeout:
                        if (m_LastChargeDuration >= m_Definition.m_ChargeTimeout)
                        {
                            m_LogSection.Record($"CancelOnTimeout");
                            CancelCharge();
                            return;
                        }
                        break;
                }

                int maxLevel = m_Definition.m_ChargedAbilityLevels.Length;

                // If we already reached the max level, exit now.
                if (m_CurrentChargeLevel >= maxLevel - 1)
                {
                    return;
                }

                float cumulativeDuration = 0;
                for (int i = 0; i < maxLevel; i++)
                {
                    ChargeLevelData level = m_Definition.m_ChargedAbilityLevels[i];
                    cumulativeDuration += level.TresholdDuration;

                    // Reach level duration treshold.
                    if (m_LastChargeDuration >= cumulativeDuration)
                    {
                        // If we already reached this level. The level effect already been activated.
                        if (m_CurrentChargeLevel >= i)
                        {
                            break;
                        }

                        // Level reached for the first time.
                        m_CurrentChargeLevel = i;
                        m_CommandQueue.Enqueue((CommandCategory)((int)CommandCategory.ChargeLevelReached + i));
                        m_LogSection.Record($"<b>On Charge Level '{i}' reached</b>");
                    }
                }
            }
        }

        private sealed class AbilityExecutionDriverCallbacks : IAbilityExecutionDriverCallbacks
        {
            private readonly Instance m_Target;
            private readonly ActionModel m_ActionModel;

            public AbilityExecutionDriverCallbacks(Instance target, ActionModel actionModel)
            {
                m_Target = target;
                m_ActionModel = actionModel;
            }

            public void OnEffectStart()
            {
                m_Target.ExecuteEffect();
            }

            public void OnEffectStop()
            {
                m_Target.StopEffect(m_ActionModel.BackgroundExecution);
            }

            public void OnExecutionComplete()
            {
                if (!m_ActionModel.TerminateExecutionOnCompletion)
                {
                    return;
                }

                m_Target.TerminateExecution();
            }
        }
    }
}