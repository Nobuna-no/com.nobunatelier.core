using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    /// <summary>
    /// Timing configuration for the awaitable execution driver.
    /// </summary>
    [Serializable]
    public class AwaitableExecutionContext
    {
        [Tooltip("Delay before OnEffectStart")]
        [Min(0)] public float ExecutionDelay = 0f;

        [Tooltip("Duration between OnEffectStart and OnEffectStop")]
        [Min(0)] public float UpdateDuration = 0.5f;

        [Tooltip("Duration between OnEffectStop and OnExecutionComplete")]
        [FormerlySerializedAs("ChainOpportunityDuration")]
        [Min(0)] public float RecoveryDuration = 0.25f;
    }

    /// <summary>
    /// Defines a single ability (move) by combining ability modules into ActionModels.
    /// Collapsed from V5's ModularAbilityDefinition hierarchy.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Ability Definition")]
    [MovedFrom(true, sourceClassName: "ModularAbilityDefinition")]
    public class AbilityDefinition : DataDefinition
    {
        [Header("Ability")]
        [SerializeField] private ActionModel m_Default;

        [Header("Charge")]
        [FormerlySerializedAs("m_canBeCharged")]
        [SerializeField] private bool m_CanBeCharged = false;

        [Tooltip("If released before the first charge level is reached, play normal modules.")]
        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        [FormerlySerializedAs("m_playAbilityOnEarlyChargeRelease")]
        private bool m_PlayAbilityOnEarlyChargeRelease = true;

        [FormerlySerializedAs("m_cancelAbilityChargeOnEarlyChargeRelease")]
        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        [Tooltip("Should Cancel Charge modules be processed when charge is released before reaching the first stage?")]
        private bool m_CancelAbilityChargeOnEarlyChargeRelease = false;

        [FormerlySerializedAs("m_chargeConstraint")]
        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        private ChargeReleaseConstraint m_ChargeConstraint = ChargeReleaseConstraint.None;

        [FormerlySerializedAs("m_chargeTimeout")]
        [SerializeField, AllowNesting, ShowIf("HasTimeoutMode")]
        private float m_ChargeTimeout = 3f;

        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        private ActionModel m_ChargeStart;

        [FormerlySerializedAs("m_chargedAbilityLevels")]
        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        private ChargeLevelData[] m_ChargedAbilityLevels;

        [SerializeField, AllowNesting, ShowIf("CanChargeCancel")]
        private ActionModel m_ChargeCancel;

        internal ActionModel Default => m_Default;
        public bool CanBeCharged => m_CanBeCharged;
        internal bool PlayAbilityOnEarlyChargeRelease => m_PlayAbilityOnEarlyChargeRelease;
        internal bool CancelAbilityChargeOnEarlyChargeRelease => m_CancelAbilityChargeOnEarlyChargeRelease;
        internal ChargeReleaseConstraint ChargeConstraint => m_ChargeConstraint;
        internal float ChargeTimeout => m_ChargeTimeout;
        internal ActionModel ChargeStart => m_ChargeStart;
        internal int ChargeLevelCount => m_ChargedAbilityLevels != null ? m_ChargedAbilityLevels.Length : 0;
        internal ChargeLevelData GetChargeLevel(int index) => m_ChargedAbilityLevels[index];
        internal ActionModel ChargeCancel => m_ChargeCancel;

#if UNITY_EDITOR
        private bool HasTimeoutMode => m_CanBeCharged
            && (m_ChargeConstraint == ChargeReleaseConstraint.ReleaseOnTimeout
            || m_ChargeConstraint == ChargeReleaseConstraint.CancelOnTimeout);
        private bool DoesCancelTimeout => m_CanBeCharged
            && m_ChargeConstraint == ChargeReleaseConstraint.CancelOnTimeout;
        private bool CanChargeCancel => DoesCancelTimeout || (m_CanBeCharged && m_CancelAbilityChargeOnEarlyChargeRelease);

        private void OnValidate()
        {
            m_Default?.Validate();
            m_ChargeStart?.Validate();
            m_ChargeCancel?.Validate();

            if (m_ChargedAbilityLevels != null)
            {
                foreach (var chargeLevel in m_ChargedAbilityLevels)
                {
                    chargeLevel.OnLevelReached?.Validate();
                    chargeLevel.OnChargeReleased?.Validate();
                }
            }
        }
#endif

        public enum ChargeReleaseConstraint
        {
            None,
            ReleaseOnMaxChargeReached,
            ReleaseOnTimeout,
            CancelOnTimeout,
        }

        [Serializable]
        internal class ChargeLevelData
        {
            [Tooltip("Cumulative duration to reach this level.")]
            [SerializeField] private float m_tresholdDuration;
            [SerializeField] private ActionModel m_OnLevelReached;
            [SerializeField] private ActionModel m_OnChargeRelease;

            public float TresholdDuration => m_tresholdDuration;
            public ActionModel OnLevelReached => m_OnLevelReached;
            public ActionModel OnChargeReleased => m_OnChargeRelease;
        }

        /// <summary>
        /// Represents a set of modules and timing settings for one phase of ability execution.
        /// Driven modules follow the execution driver's lifecycle; overlay modules are tied to the ActionModel's lifetime.
        /// </summary>
        [Serializable]
        public class ActionModel
        {
            [Tooltip("Module responsible for execution timing. If not set, AwaitableExecutionDriver is used.")]
            [SerializeReference, AllowNesting, ShowIf("UsesExecutionDriver"), ReadOnly]
            private AbilityModuleDefinition m_ExecutionDriverModule;

            [Tooltip("Timing context for the awaitable execution driver.")]
            [SerializeField, AllowNesting, HideIf("UsesExecutionDriver")]
            private AwaitableExecutionContext m_AwaitableExecutionContext;

            [FormerlySerializedAs("m_Modules")]
            [SerializeField] private AbilityModuleDefinition[] m_DrivenModules;

            [SerializeField] private AbilityModuleDefinition[] m_OverlayModules;

            public IReadOnlyList<AbilityModuleDefinition> DrivenModules => m_DrivenModules;
            public IReadOnlyList<AbilityModuleDefinition> OverlayModules => m_OverlayModules;
            public AbilityModuleDefinition ExecutionDriverModule => m_ExecutionDriverModule;
            public float ExecutionDelay => m_AwaitableExecutionContext != null ? m_AwaitableExecutionContext.ExecutionDelay : 0f;
            public float UpdateDuration => m_AwaitableExecutionContext != null ? m_AwaitableExecutionContext.UpdateDuration : 0f;
            public float RecoveryDuration => m_AwaitableExecutionContext != null ? m_AwaitableExecutionContext.RecoveryDuration : 0f;

            internal AbilityModuleDefinition[] GetDrivenModulesArray() => m_DrivenModules;
            internal AbilityModuleDefinition[] GetOverlayModulesArray() => m_OverlayModules;

#if UNITY_EDITOR
            private bool UsesExecutionDriver => m_ExecutionDriverModule != null;

            internal void Validate()
            {
                int executionDriverModuleIndexToRemove = -1;
                m_ExecutionDriverModule = null;

                if (m_DrivenModules == null)
                {
                    return;
                }

                for (int i = 0; i < m_DrivenModules.Length; i++)
                {
                    if (m_ExecutionDriverModule == null)
                    {
                        if (m_DrivenModules[i] != null && m_DrivenModules[i] is IAbilityExecutionDriverModuleDefinition)
                        {
                            m_ExecutionDriverModule = m_DrivenModules[i];
                            continue;
                        }
                    }
                    else if (m_DrivenModules[i] is IAbilityExecutionDriverModuleDefinition)
                    {
                        executionDriverModuleIndexToRemove = i;
                        break;
                    }
                }

                if (executionDriverModuleIndexToRemove != -1)
                {
                    Debug.LogWarning($"Multiple execution driver modules found. Removing: {m_DrivenModules[executionDriverModuleIndexToRemove].name}.");
                    m_DrivenModules[executionDriverModuleIndexToRemove] = null;
                }
            }
#endif
        }
    }
}
