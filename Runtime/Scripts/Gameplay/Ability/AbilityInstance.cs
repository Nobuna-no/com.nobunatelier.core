using System;
using System.Collections.Generic;
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
    /// Single-authority runtime for one ability execution.
    /// Owns the state machine. Drivers signal phase transitions, AbilityInstance decides what to do.
    /// </summary>
    internal class AbilityInstance
    {
        private ExecutionState m_State = ExecutionState.Ready;
        private SkillDefinition m_CurrentSkill;
        private ActionExecution m_ActiveAction;
        private AbilityExecutionContext m_ExecutionContext;
        private CancellationTokenSource m_ExecutionCts;
        private CancellationToken m_ControllerToken;
        private AbilityController m_Controller;
        private ContextualLogManager.LogPartition m_Log;

        // Charge state
        private bool m_IsCharging;
        private int m_CurrentChargeLevel = -1;
        private float m_ChargeDuration;

        public event Action OnAbilityStarted;
        public event Action OnAbilityStartCharge;
        public event Action OnRecoveryWindowOpen;
        public event Action OnAbilityCompleted;
        public event Action OnAbilityCancelled;

        public SkillDefinition CurrentSkill => m_CurrentSkill;
        public ExecutionState State => m_State;
        public bool IsCharging => m_IsCharging;
        public bool IsInRecovery => m_State == ExecutionState.Recovery;
        public AbilityExecutionContext ExecutionContext => m_ExecutionContext;

        public AbilityInstance(AbilityController controller)
        {
            m_Controller = controller;
            m_ControllerToken = controller.destroyCancellationToken;
            m_Log = controller.Log;
        }

        public bool TryExecute(SkillDefinition skill, AbilityExecutionContext? context = null)
        {
            if (m_State != ExecutionState.Ready && m_State != ExecutionState.Recovery)
                return false;

            m_Log?.Record($"TryExecute: {skill.name}");

            m_CurrentSkill = skill;
            m_ExecutionContext = context ?? default;
            ResetChargeState();

            return ActivateAbilityAction(skill.DefaultAction);
        }

        public bool StartCharge(SkillDefinition skill)
        {
            m_Log?.Record($"StartCharge: {skill.name}");

            if (m_State == ExecutionState.InProgress)
            {
                m_Log?.Record("StartCharge aborted — ability in progress.",
                    ContextualLogManager.LogTypeFilter.Warning);
                return false;
            }

            if (skill.Mode != SkillDefinition.SkillMode.Hold)
            {
                m_Log?.Record("Skill is not Hold mode. Playing default.",
                    ContextualLogManager.LogTypeFilter.Warning);
                return TryExecute(skill);
            }

            if (m_State != ExecutionState.Ready && m_State != ExecutionState.Recovery)
                return false;

            TeardownActiveAction();
            m_CurrentSkill = skill;
            m_ExecutionContext = default;

            m_IsCharging = true;
            m_CurrentChargeLevel = -1;
            m_ChargeDuration = 0f;
            m_State = ExecutionState.Charging;

            if (skill.Hold?.HoldStartAction != null)
            {
                ActivateOverlayAction(skill.Hold.HoldStartAction);
            }

            OnAbilityStartCharge?.Invoke();
            return true;
        }

        public void ReleaseCharge()
        {
            if (!m_IsCharging)
            {
                m_Log?.Record("ReleaseCharge ignored — not charging.");
                return;
            }

            m_Log?.Record($"ReleaseCharge (level={m_CurrentChargeLevel})");

            if (m_CurrentChargeLevel < 0)
            {
                m_Log?.Record("Early release -> DefaultAction");
                m_IsCharging = false;
                m_State = ExecutionState.Ready;
                ActivateAbilityAction(m_CurrentSkill.DefaultAction);
                return;
            }

            var levels = m_CurrentSkill.Hold?.HoldLevels;
            if (levels != null && m_CurrentChargeLevel < levels.Length)
            {
                var releaseAction = levels[m_CurrentChargeLevel].OnReleased;
                m_IsCharging = false;
                m_State = ExecutionState.Ready;

                if (releaseAction != null)
                {
                    ActivateAbilityAction(releaseAction);
                }
                else
                {
                    m_Log?.Record("No OnReleased action for charge level. Playing default.");
                    ActivateAbilityAction(m_CurrentSkill.DefaultAction);
                }
            }
        }

        public void CancelCharge()
        {
            if (!m_IsCharging)
            {
                m_Log?.Record("CancelCharge ignored — not charging.");
                return;
            }

            m_Log?.Record("CancelCharge");
            CancelChargeInternal();
        }

        public void Cancel()
        {
            if (m_State == ExecutionState.Ready && m_ActiveAction == null)
                return;

            m_Log?.Record("Cancel");

            m_ActiveAction?.Cancel();
            m_ActiveAction = null;

            CancelExecutionToken();
            ResetChargeState();
            m_State = ExecutionState.Ready;
            OnAbilityCancelled?.Invoke();
        }

        public void Update(float deltaTime)
        {
            switch (m_State)
            {
                case ExecutionState.Charging:
                    m_ActiveAction?.Update(deltaTime);
                    UpdateChargeLevel(deltaTime);
                    break;

                case ExecutionState.Starting:
                case ExecutionState.InProgress:
                case ExecutionState.Recovery:
                    m_ActiveAction?.Update(deltaTime);
                    break;
            }
        }

        public void Dispose()
        {
            if (m_State != ExecutionState.Ready)
            {
                m_ActiveAction?.Cancel();
                m_ActiveAction = null;
                ResetChargeState();
                m_State = ExecutionState.Ready;
            }

            CancelExecutionToken();
        }

        #region Phase Transitions (called by ActionExecution)

        internal void HandlePhaseTransition(AbilityPhase phase)
        {
            switch (phase)
            {
                case AbilityPhase.Active:
                    if (m_State != ExecutionState.Starting)
                        return;
                    m_Log?.Record("Phase -> Active: Starting -> InProgress");
                    m_State = ExecutionState.InProgress;
                    break;

                case AbilityPhase.Recovery:
                    if (m_State != ExecutionState.InProgress)
                        return;
                    m_Log?.Record("Phase -> Recovery: InProgress -> Recovery");
                    m_State = ExecutionState.Recovery;
                    OnRecoveryWindowOpen?.Invoke();
                    break;

                case AbilityPhase.Complete:
                    if (m_State != ExecutionState.Recovery)
                        return;
                    m_Log?.Record("Phase -> Complete: Recovery -> Ready");
                    m_ActiveAction?.Teardown();
                    m_ActiveAction = null;
                    CancelExecutionToken();
                    m_State = ExecutionState.Ready;
                    OnAbilityCompleted?.Invoke();
                    break;
            }
        }

        #endregion

        #region Charge Internals

        private void UpdateChargeLevel(float deltaTime)
        {
            if (!m_IsCharging || m_CurrentSkill?.Hold == null)
                return;

            var hold = m_CurrentSkill.Hold;

            switch (hold.Constraint)
            {
                case SkillDefinition.HoldConstraint.ReleaseOnMaxChargeReached:
                    if (hold.HoldLevels != null && m_CurrentChargeLevel >= hold.HoldLevels.Length - 1)
                    {
                        m_Log?.Record("ReleaseOnMaxChargeReached");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case SkillDefinition.HoldConstraint.ReleaseOnTimeout:
                    if (m_ChargeDuration >= hold.Timeout)
                    {
                        m_Log?.Record("ReleaseOnTimeout");
                        ReleaseCharge();
                        return;
                    }
                    break;

                case SkillDefinition.HoldConstraint.CancelOnTimeout:
                    if (m_ChargeDuration >= hold.Timeout)
                    {
                        m_Log?.Record("CancelOnTimeout");
                        CancelCharge();
                        return;
                    }
                    break;
            }

            m_ChargeDuration += deltaTime;

            var levels = hold.HoldLevels;
            if (levels == null || m_CurrentChargeLevel >= levels.Length - 1)
                return;

            float cumulativeDuration = 0f;
            for (int i = 0; i < levels.Length; i++)
            {
                cumulativeDuration += levels[i].ThresholdDuration;

                if (m_ChargeDuration >= cumulativeDuration && m_CurrentChargeLevel < i)
                {
                    m_CurrentChargeLevel = i;
                    m_Log?.Record($"Charge level {i} reached");

                    if (levels[i].OnLevelReached != null)
                    {
                        ActivateOverlayAction(levels[i].OnLevelReached);
                    }
                }
            }
        }

        private void CancelChargeInternal()
        {
            TeardownActiveAction();

            if (m_CurrentSkill?.Hold?.HoldCancelAction != null)
            {
                ActivateOverlayAction(m_CurrentSkill.Hold.HoldCancelAction);
            }

            ResetChargeState();
            m_State = ExecutionState.Ready;
            OnAbilityCancelled?.Invoke();
        }

        private void ResetChargeState()
        {
            m_IsCharging = false;
            m_CurrentChargeLevel = -1;
            m_ChargeDuration = 0f;
        }

        #endregion

        private bool ActivateAbilityAction(AbilityActionData action)
        {
            TeardownActiveAction();
            CancelExecutionToken();

            m_ExecutionCts = m_ControllerToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(m_ControllerToken)
                : new CancellationTokenSource();

            m_ActiveAction = new ActionExecution();

            // State must be Starting before Activate — if StartupDuration is 0,
            // the driver fires Active synchronously during Activate.
            m_State = ExecutionState.Starting;

            bool hasDriver = m_ActiveAction.Activate(
                action, m_Controller, m_CurrentSkill, m_ExecutionCts.Token, this,
                isOverlay: false);

            if (hasDriver)
            {
                m_ActiveAction.FireStartEffects(m_CurrentSkill, m_Controller);
                OnAbilityStarted?.Invoke();
                return true;
            }

            m_ActiveAction = null;
            m_State = ExecutionState.Ready;
            return false;
        }

        /// <summary>
        /// Overlay action — driver plays but phase transitions are ignored.
        /// Used for charge start, charge cancel, and level-reached animations.
        /// </summary>
        private void ActivateOverlayAction(AbilityActionData action)
        {
            TeardownActiveAction();

            m_ActiveAction = new ActionExecution();
            m_ActiveAction.Activate(
                action, m_Controller, m_CurrentSkill, default, this,
                isOverlay: true);
        }

        private void TeardownActiveAction()
        {
            if (m_ActiveAction != null)
            {
                m_ActiveAction.Teardown();
                m_ActiveAction = null;
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

        /// <summary>
        /// Manages one AbilityAction's runtime: driver + phase effects + content event dispatch.
        /// </summary>
        private class ActionExecution : IAbilityActionDriverCallbacks
        {
            private IAbilityActionDriver m_Driver;
            private AbilityActionData m_Action;
            private AbilityInstance m_Owner;
            private AbilityController m_Controller;
            private float m_SkillValue;
            private bool m_IsOverlay;
            private ContextualLogManager.LogPartition m_Log;

            // Content event -> bound effect entries
            private Dictionary<GameplayEventDefinition, List<(EffectEntry entry, IAbilityEffectInstance instance)>> m_EventMap;

            // Phase -> effect instances (created on Activate, fired on phase transition)
            private Dictionary<AbilityPhase, List<(EffectEntry entry, IAbilityEffectInstance instance)>> m_PhaseMap;

            // Start effects — fired immediately when ability starts (not a driver phase)
            private List<(EffectEntry entry, IAbilityEffectInstance instance)> m_StartEffects;

            private List<IAbilityEffectInstance> m_UpdatingInstances;

            public bool Activate(AbilityActionData action, AbilityController controller,
                SkillDefinition skill, CancellationToken token, AbilityInstance owner,
                bool isOverlay)
            {
                m_Action = action;
                m_Owner = owner;
                m_Controller = controller;
                m_SkillValue = skill?.Value ?? 0f;
                m_Log = controller.Log;
                m_IsOverlay = isOverlay;

                m_EventMap = new Dictionary<GameplayEventDefinition, List<(EffectEntry, IAbilityEffectInstance)>>();
                m_PhaseMap = new Dictionary<AbilityPhase, List<(EffectEntry, IAbilityEffectInstance)>>();
                m_StartEffects = new List<(EffectEntry, IAbilityEffectInstance)>();
                m_UpdatingInstances = new List<IAbilityEffectInstance>();

                // Build phase + start effect maps
                BuildEffectList(action?.OnStartEffects, m_StartEffects, controller);
                BuildPhaseEntries(AbilityPhase.Active, action?.OnActiveEffects, controller);
                BuildPhaseEntries(AbilityPhase.Recovery, action?.OnRecoveryEffects, controller);

                // Build content event map
                if (action?.GameplayEvents != null)
                {
                    foreach (var group in action.GameplayEvents)
                    {
                        if (group.Event == null || group.Effects == null)
                            continue;

                        foreach (var entry in group.Effects)
                        {
                            var effect = entry.Resolved;
                            if (effect == null)
                                continue;

                            var instance = effect.CreateInstance(controller);

                            if (!m_EventMap.TryGetValue(group.Event, out var list))
                            {
                                list = new List<(EffectEntry, IAbilityEffectInstance)>();
                                m_EventMap[group.Event] = list;
                            }
                            list.Add((entry, instance));
                        }
                    }
                }

                // Initialize and start driver
                m_Driver = action?.Driver;
                if (m_Driver == null)
                {
                    if (!isOverlay)
                    {
                        m_Log?.Record("ActionExecution: No driver on AbilityAction.",
                            ContextualLogManager.LogTypeFilter.Warning);
                    }
                    return false;
                }

                var context = new AbilityActionDriverContext(this, token, controller);
                m_Driver.Initialize(in context);
                m_Driver.RequestExecution();
                return true;
            }

            /// <summary>
            /// Fire start effects — called immediately when ability enters Starting state.
            /// </summary>
            public void FireStartEffects(SkillDefinition skill, AbilityController controller)
            {
                ExecuteEffectList(m_StartEffects, controller);
            }

            /// <summary>
            /// Fire phase-bound effects for the given phase.
            /// Called by ActionExecution.OnPhaseTransition before state transition.
            /// </summary>
            private void FirePhaseEffects(AbilityPhase phase, AbilityController controller)
            {
                if (!m_PhaseMap.TryGetValue(phase, out var entries))
                    return;

                ExecuteEffectList(entries, controller);
            }

            private void ExecuteEffectList(List<(EffectEntry entry, IAbilityEffectInstance instance)> entries,
                AbilityController controller)
            {
                if (entries == null)
                    return;

                foreach (var (entry, instance) in entries)
                {
                    if (instance == null)
                        continue;

                    var ctx = new AbilityEffectContext(
                        m_SkillValue * entry.ValueMultiplier,
                        entry.Target,
                        controller);

                    instance.Execute(ctx);

                    if (instance.NeedsUpdate)
                        m_UpdatingInstances.Add(instance);
                }
            }

            // IAbilityActionDriverCallbacks — content events only
            public void FireEvent(GameplayEventDefinition gameplayEvent)
            {
                m_Log?.Record($"ActionExecution.FireEvent: {gameplayEvent?.name ?? "null"}");

                if (!m_EventMap.TryGetValue(gameplayEvent, out var entries))
                    return;

                foreach (var (entry, instance) in entries)
                {
                    if (instance == null)
                        continue;

                    var ctx = new AbilityEffectContext(
                        m_SkillValue * entry.ValueMultiplier,
                        entry.Target,
                        m_Controller);

                    instance.Execute(ctx);

                    if (instance.NeedsUpdate)
                        m_UpdatingInstances.Add(instance);
                }
            }

            // IAbilityActionDriverCallbacks — phase transitions
            public void OnPhaseTransition(AbilityPhase phase)
            {
                m_Log?.Record($"ActionExecution.OnPhaseTransition: {phase}");

                // Fire phase effects regardless of overlay status
                FirePhaseEffects(phase, m_Controller);

                // Only primary actions transition the state machine
                if (!m_IsOverlay)
                {
                    m_Owner.HandlePhaseTransition(phase);
                }
            }

            public void Update(float deltaTime)
            {
                for (int i = m_UpdatingInstances.Count - 1; i >= 0; i--)
                {
                    m_UpdatingInstances[i].Update(deltaTime);
                }
            }

            public void Teardown()
            {
                StopAllInstances();
                m_Driver?.Reset();
                m_Driver = null;
            }

            public void Cancel()
            {
                StopAllInstances();
                m_Driver?.Cancel();
                m_Driver = null;
            }

            private void BuildPhaseEntries(AbilityPhase phase, IReadOnlyList<EffectEntry> entries,
                AbilityController controller)
            {
                if (entries == null)
                    return;

                if (!m_PhaseMap.TryGetValue(phase, out var list))
                {
                    list = new List<(EffectEntry, IAbilityEffectInstance)>();
                    m_PhaseMap[phase] = list;
                }

                BuildEffectList(entries, list, controller);
            }

            private static void BuildEffectList(IReadOnlyList<EffectEntry> entries,
                List<(EffectEntry, IAbilityEffectInstance)> target, AbilityController controller)
            {
                if (entries == null)
                    return;

                foreach (var entry in entries)
                {
                    var effect = entry.Resolved;
                    if (effect == null)
                        continue;

                    var instance = effect.CreateInstance(controller);
                    target.Add((entry, instance));
                }
            }

            private void StopAllInstances()
            {
                StopList(m_StartEffects);
                m_StartEffects?.Clear();

                if (m_EventMap != null)
                {
                    foreach (var list in m_EventMap.Values)
                        StopList(list);
                    m_EventMap.Clear();
                }

                if (m_PhaseMap != null)
                {
                    foreach (var list in m_PhaseMap.Values)
                        StopList(list);
                    m_PhaseMap.Clear();
                }

                m_UpdatingInstances?.Clear();
            }

            private static void StopList(List<(EffectEntry entry, IAbilityEffectInstance instance)> list)
            {
                if (list == null)
                    return;

                foreach (var (_, instance) in list)
                    instance?.Stop();
            }
        }
    }
}
