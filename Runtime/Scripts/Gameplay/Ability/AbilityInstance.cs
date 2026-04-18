using System;
using System.Threading;

namespace NobunAtelier
{
    public enum ExecutionState
    {
        Ready,
        Starting,
        InProgress,
        Recovery,
        Charging,
    }

    /// <summary>
    /// Single-authority runtime for one ability execution. Merges V5's Instance, AbilityStateMachine,
    /// and AbilityCommandQueue into one class. Reusable across definition switches.
    /// </summary>
    internal class AbilityInstance : IAbilityExecutionDriverCallbacks
    {
        private ExecutionState m_State = ExecutionState.Ready;
        private AbilityDefinition m_CurrentDefinition;
        private CommandExecution m_ActiveCommand;
        private AbilityModuleRegistry m_ModuleRegistry;
        private AbilityExecutionContext m_ExecutionContext;
        private CancellationTokenSource m_ExecutionCts;
        private CancellationToken m_ControllerToken;
        private ContextualLogManager.LogPartition m_Log;

        private bool m_IsCharging;
        private int m_CurrentChargeLevel = -1;
        private float m_LastChargeDuration;

        public event Action OnAbilityStarted;
        public event Action OnAbilityStartCharge;
        public event Action OnRecoveryWindowOpen;
        public event Action OnAbilityCompleted;
        public event Action OnAbilityCancelled;

        public AbilityDefinition CurrentAbility => m_CurrentDefinition;
        public ExecutionState State => m_State;
        public bool IsCharging => m_IsCharging;
        public bool IsInRecovery => m_State == ExecutionState.Recovery;
        public AbilityExecutionContext ExecutionContext => m_ExecutionContext;

        public AbilityInstance(AbilityController controller)
        {
            m_ModuleRegistry = new AbilityModuleRegistry(controller);
            m_ControllerToken = controller.destroyCancellationToken;
            m_Log = controller.Log;
        }

        public bool TryExecute(AbilityDefinition definition, AbilityExecutionContext? context = null)
        {
            if (m_State != ExecutionState.Ready && m_State != ExecutionState.Recovery)
            {
                return false;
            }

            m_Log?.Record($"TryExecute: {definition.name}");

            TeardownActiveCommand();
            m_CurrentDefinition = definition;
            m_ExecutionContext = context ?? default;
            m_IsCharging = false;

            ActivateActionModel(definition.Default);
            return true;
        }

        public void Cancel()
        {
            if (m_State == ExecutionState.Ready && m_ActiveCommand == null)
            {
                return;
            }

            m_Log?.Record("Cancel");

            m_ActiveCommand?.Cancel();
            m_ActiveCommand = null;

            CancelExecutionToken();
            ResetChargeState();
            m_State = ExecutionState.Ready;
            OnAbilityCancelled?.Invoke();
        }

        public bool StartCharge(AbilityDefinition definition)
        {
            m_Log?.Record($"StartCharge: {definition.name}");

            if (m_State == ExecutionState.InProgress)
            {
                m_Log?.Record("Aborted - ability is in progress.", ContextualLogManager.LogTypeFilter.Warning);
                return false;
            }

            if (!definition.CanBeCharged)
            {
                m_Log?.Record("Ability cannot be charged. Playing default.", ContextualLogManager.LogTypeFilter.Warning);
                return TryExecute(definition);
            }

            if (m_State != ExecutionState.Ready && m_State != ExecutionState.Recovery)
            {
                return false;
            }

            TeardownActiveCommand();
            m_CurrentDefinition = definition;
            m_ExecutionContext = default;

            m_IsCharging = true;
            m_CurrentChargeLevel = -1;
            m_LastChargeDuration = 0f;
            m_State = ExecutionState.Charging;

            if (definition.ChargeStart != null)
            {
                ActivateOverlayCommand(definition.ChargeStart);
            }

            OnAbilityStartCharge?.Invoke();
            return true;
        }

        public void ReleaseCharge()
        {
            if (!m_IsCharging)
            {
                m_Log?.Record("ReleaseCharge ignored - not charging.");
                return;
            }

            m_Log?.Record("ReleaseCharge");

            if (m_CurrentChargeLevel < 0)
            {
                if (m_CurrentDefinition.CancelAbilityChargeOnEarlyChargeRelease)
                {
                    m_Log?.Record("Cancel charge on early release.");
                    CancelChargeInternal();
                }

                if (m_CurrentDefinition.PlayAbilityOnEarlyChargeRelease)
                {
                    m_Log?.Record("Play default on early release.");
                    m_IsCharging = false;
                    m_State = ExecutionState.Ready;
                    ActivateActionModel(m_CurrentDefinition.Default);
                }

                return;
            }

            var releaseModel = m_CurrentDefinition.GetChargeLevel(m_CurrentChargeLevel).OnChargeReleased;
            m_IsCharging = false;
            m_State = ExecutionState.Ready;
            ActivateActionModel(releaseModel);
        }

        public void CancelCharge()
        {
            if (!m_IsCharging)
            {
                m_Log?.Record("CancelCharge failed - not charging.");
                return;
            }

            m_Log?.Record("CancelCharge");
            CancelChargeInternal();
        }

        public void Update(float deltaTime)
        {
            if (m_State == ExecutionState.Charging)
            {
                m_ActiveCommand?.Update(deltaTime);
                UpdateChargeLevel(deltaTime);
                return;
            }

            if (m_State == ExecutionState.Starting ||
                m_State == ExecutionState.InProgress ||
                m_State == ExecutionState.Recovery)
            {
                m_ActiveCommand?.Update(deltaTime);
            }
        }

        public void Dispose()
        {
            if (m_State != ExecutionState.Ready)
            {
                m_ActiveCommand?.Cancel();
                m_ActiveCommand = null;
                ResetChargeState();
                m_State = ExecutionState.Ready;
            }

            CancelExecutionToken();
        }

        #region IAbilityExecutionDriverCallbacks

        void IAbilityExecutionDriverCallbacks.OnEffectStart()
        {
            if (m_State != ExecutionState.Starting)
            {
                return;
            }

            m_Log?.Record("Driver → OnEffectStart: Starting → InProgress");
            m_State = ExecutionState.InProgress;
            m_ActiveCommand?.ExecuteDrivenModules();
        }

        void IAbilityExecutionDriverCallbacks.OnEffectStop()
        {
            if (m_State != ExecutionState.InProgress)
            {
                return;
            }

            m_Log?.Record("Driver → OnEffectStop: InProgress → Recovery");
            m_State = ExecutionState.Recovery;
            m_ActiveCommand?.StopDrivenModules();
            OnRecoveryWindowOpen?.Invoke();
        }

        void IAbilityExecutionDriverCallbacks.OnExecutionComplete()
        {
            if (m_State != ExecutionState.Recovery)
            {
                return;
            }

            m_Log?.Record("Driver → OnExecutionComplete: Recovery → Ready");
            m_ActiveCommand?.StopOverlayModules();
            m_ActiveCommand = null;
            CancelExecutionToken();
            m_State = ExecutionState.Ready;
            OnAbilityCompleted?.Invoke();
        }

        #endregion

        private void ActivateActionModel(AbilityDefinition.ActionModel actionModel)
        {
            TeardownActiveCommand();
            CancelExecutionToken();
            m_ExecutionCts = m_ControllerToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(m_ControllerToken)
                : new CancellationTokenSource();

            m_ActiveCommand = new CommandExecution();
            bool hasDriver = m_ActiveCommand.Activate(actionModel, m_ModuleRegistry, this, m_ExecutionCts.Token, m_Log);

            if (hasDriver)
            {
                m_State = ExecutionState.Starting;
                OnAbilityStarted?.Invoke();
            }
        }

        private void ActivateOverlayCommand(AbilityDefinition.ActionModel actionModel)
        {
            TeardownActiveCommand();
            m_ActiveCommand = new CommandExecution();
            m_ActiveCommand.ActivateOverlayOnly(actionModel, m_ModuleRegistry);
        }

        private void TeardownActiveCommand()
        {
            if (m_ActiveCommand != null)
            {
                m_ActiveCommand.Teardown();
                m_ActiveCommand = null;
            }
        }

        private void CancelExecutionToken()
        {
            if (m_ExecutionCts != null)
            {
                m_ExecutionCts.Cancel();
                m_ExecutionCts.Dispose();
                m_ExecutionCts = null;
            }
        }

        private void ResetChargeState()
        {
            m_IsCharging = false;
            m_CurrentChargeLevel = -1;
            m_LastChargeDuration = 0f;
        }

        private void CancelChargeInternal()
        {
            TeardownActiveCommand();

            if (m_CurrentDefinition.ChargeCancel != null)
            {
                ActivateOverlayCommand(m_CurrentDefinition.ChargeCancel);
            }

            m_CurrentChargeLevel = -1;
            m_State = ExecutionState.Ready;
            m_IsCharging = false;
        }

        private void UpdateChargeLevel(float deltaTime)
        {
            if (!m_IsCharging || m_CurrentDefinition == null)
            {
                return;
            }

            switch (m_CurrentDefinition.ChargeConstraint)
            {
                case AbilityDefinition.ChargeReleaseConstraint.ReleaseOnMaxChargeReached:
                    if (m_CurrentChargeLevel >= m_CurrentDefinition.ChargeLevelCount - 1)
                    {
                        m_Log?.Record("ReleaseOnMaxChargeReached");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case AbilityDefinition.ChargeReleaseConstraint.ReleaseOnTimeout:
                    if (m_LastChargeDuration >= m_CurrentDefinition.ChargeTimeout)
                    {
                        m_Log?.Record("ReleaseOnTimeout");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case AbilityDefinition.ChargeReleaseConstraint.CancelOnTimeout:
                    if (m_LastChargeDuration >= m_CurrentDefinition.ChargeTimeout)
                    {
                        m_Log?.Record("CancelOnTimeout");
                        CancelCharge();
                        return;
                    }
                    break;
            }

            m_LastChargeDuration += deltaTime;

            int maxLevel = m_CurrentDefinition.ChargeLevelCount;
            if (m_CurrentChargeLevel >= maxLevel - 1)
            {
                return;
            }

            float cumulativeDuration = 0f;
            for (int i = 0; i < maxLevel; i++)
            {
                var level = m_CurrentDefinition.GetChargeLevel(i);
                cumulativeDuration += level.TresholdDuration;

                if (m_LastChargeDuration >= cumulativeDuration)
                {
                    if (m_CurrentChargeLevel >= i)
                    {
                        break;
                    }

                    m_CurrentChargeLevel = i;
                    m_Log?.Record($"Charge level {i} reached");

                    if (level.OnLevelReached != null)
                    {
                        ActivateOverlayCommand(level.OnLevelReached);
                    }
                }
            }
        }

        /// <summary>
        /// Manages driven + overlay modules for one ActionModel activation.
        /// </summary>
        private class CommandExecution
        {
            private IAbilityExecutionDriver m_Driver;
            private AbilityModuleDefinition[] m_DrivenModules;
            private AbilityModuleDefinition[] m_OverlayModules;
            private AbilityModuleRegistry m_Registry;

            public bool Activate(AbilityDefinition.ActionModel actionModel, AbilityModuleRegistry registry,
                IAbilityExecutionDriverCallbacks callbacks, CancellationToken token,
                ContextualLogManager.LogPartition log)
            {
                m_Registry = registry;
                m_DrivenModules = actionModel.GetDrivenModulesArray();
                m_OverlayModules = actionModel.GetOverlayModulesArray();

                RegisterModules(m_DrivenModules);
                RegisterModules(m_OverlayModules);

                InitiateAndExecuteModules(m_OverlayModules);

                bool hasDriven = m_DrivenModules != null && m_DrivenModules.Length > 0;

                if (actionModel.ExecutionDriverModule != null)
                {
                    if (registry.m_ModulesMap.TryGetValue(actionModel.ExecutionDriverModule, out var instance))
                    {
                        m_Driver = instance as IAbilityExecutionDriver;
                        if (m_Driver == null)
                        {
                            log?.Record("Driver module does not implement IAbilityExecutionDriver.",
                                ContextualLogManager.LogTypeFilter.Warning);
                        }
                    }
                    else
                    {
                        log?.Record("Driver module instance not found in registry.",
                            ContextualLogManager.LogTypeFilter.Warning);
                    }
                }

                if (m_Driver == null && hasDriven)
                {
                    var fallback = new AwaitableExecutionDriver();
                    fallback.Configure(actionModel.ExecutionDelay, actionModel.UpdateDuration, actionModel.RecoveryDuration);
                    m_Driver = fallback;
                }

                if (m_Driver != null)
                {
                    InitiateModules(m_DrivenModules);
                    m_Driver.Initialize(new AbilityExecutionDriverContext(callbacks, token));
                    m_Driver.RequestExecution();
                    return true;
                }

                return false;
            }

            public void ActivateOverlayOnly(AbilityDefinition.ActionModel actionModel, AbilityModuleRegistry registry)
            {
                m_Registry = registry;
                m_OverlayModules = actionModel.GetOverlayModulesArray();
                m_DrivenModules = null;

                RegisterModules(m_OverlayModules);
                InitiateAndExecuteModules(m_OverlayModules);
            }

            public void ExecuteDrivenModules()
            {
                if (m_DrivenModules != null && m_Registry != null)
                {
                    m_Registry.ExecuteModules(m_DrivenModules);
                }
            }

            public void StopDrivenModules()
            {
                if (m_DrivenModules != null && m_Registry != null)
                {
                    m_Registry.StopModules(m_DrivenModules);
                }
            }

            public void StopOverlayModules()
            {
                if (m_OverlayModules != null && m_Registry != null)
                {
                    m_Registry.StopModules(m_OverlayModules);
                }
            }

            public void Update(float deltaTime)
            {
                if (m_DrivenModules != null && m_DrivenModules.Length > 0 && m_Registry != null)
                {
                    m_Registry.UpdateModules(deltaTime, m_DrivenModules);
                }

                if (m_OverlayModules != null && m_OverlayModules.Length > 0 && m_Registry != null)
                {
                    m_Registry.UpdateModules(deltaTime, m_OverlayModules);
                }
            }

            public void Teardown()
            {
                StopOverlayModules();
                m_Driver?.Reset();
                m_Driver = null;
            }

            public void Cancel()
            {
                m_Driver?.Cancel();
                m_Driver = null;
                StopDrivenModules();
                StopOverlayModules();
            }

            private void RegisterModules(AbilityModuleDefinition[] modules)
            {
                if (modules == null || m_Registry == null)
                {
                    return;
                }

                m_Registry.Add(modules);
            }

            private void InitiateModules(AbilityModuleDefinition[] modules)
            {
                if (modules == null || m_Registry == null)
                {
                    return;
                }

                foreach (var mod in modules)
                {
                    if (mod != null && m_Registry.m_ModulesMap.TryGetValue(mod, out var instance))
                    {
                        instance.InitiateExecution();
                    }
                }
            }

            private void InitiateAndExecuteModules(AbilityModuleDefinition[] modules)
            {
                if (modules == null || m_Registry == null)
                {
                    return;
                }

                foreach (var mod in modules)
                {
                    if (mod != null && m_Registry.m_ModulesMap.TryGetValue(mod, out var instance))
                    {
                        instance.InitiateExecution();
                        instance.ExecuteEffect();
                    }
                }
            }
        }
    }
}
