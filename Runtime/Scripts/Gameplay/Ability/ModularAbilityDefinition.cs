using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [CreateAssetMenu]
    public partial class ModularAbilityDefinition : AbilityDefinition
    {
        [Header("Modular Ability")]
        [InfoBox("This assets defines an Ability by combining 'Ability Modules'. " +
            "Each module type defines a specific effect or behavior (i.e. FXs, Hitboxes, Animation, ...). " +
            "Each set of module (Default, StartCharge, CancelCharge, ...) are compiles into commands " +
            "that follows the same lifecyle:" +
            "\n1. Initiate: Ability modules start execution" +
            "\n2. Execute: Ability modules effect starts" +
            "\n3. Update: Ability modules effect update" +
            "\n4. Stop: Ability modules effect stop" +
            "\n" +
            "\nAt the higher level, the AbilityInstance handle its own state machine that wraps the same lifecycle." +
            "\nThe system raises several event that can be found on the AbilityController:" +
            "\n- OnAbilityStartCharge: Raised when ability instance StartCharge is called" +
            "\n- OnAbilityStartExecution: Raised every time a command (non quiet) is Initiated" +
            "\n- OnAbilityChainOpportunity: Raised every time a command is stopped" +
            "\n- OnAbilityExecutionCompletion: Raised every time a command reach end of the ChainOpportunity timing",
            type: (EInfoBoxType)3)]
        [SerializeField] private ActionModel m_Default;
        [FormerlySerializedAs("m_canChainOnSelf")]
        [SerializeField] private bool m_CanChainOnSelf = true;

        [Header("Charge")]
        [FormerlySerializedAs("m_canBeCharged")]
        [SerializeField] private bool m_CanBeCharged = false;
        [Tooltip("If released before the first charge level is reached, play normal modules.")]
        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        [FormerlySerializedAs("m_playAbilityOnEarlyChargeRelease")]
        private bool m_PlayAbilityOnEarlyChargeRelease = true;

        [FormerlySerializedAs("m_cancelAbilityChargeOnEarlyChargeRelease")]
        [SerializeField, AllowNesting, ShowIf("m_CanBeCharged")]
        [Tooltip("Should Cancel Charge modules be processed in case charge is released before reaching the first stage?\n" +
            "Processing happens before Default ability processing in case 'PlayAbilityOnEarlyChargeRelease' is true.")]
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
        public ActionModel m_ChargeCancel;

#if UNITY_EDITOR
        private bool HasTimeoutMode => m_CanBeCharged
            && (m_ChargeConstraint == ChargeReleaseConstraint.ReleaseOnTimeout
            || m_ChargeConstraint == ChargeReleaseConstraint.CancelOnTimeout);
        private bool DoesCancelTimeout => m_CanBeCharged
            && m_ChargeConstraint == ChargeReleaseConstraint.CancelOnTimeout;
        private bool CanChargeCancel => DoesCancelTimeout || (m_CanBeCharged && m_CancelAbilityChargeOnEarlyChargeRelease);

        private void OnValidate()
        {
            if (m_Default != null)
            {
                m_Default.Validate();
            }
            if (m_ChargeStart != null)
            {
                m_ChargeStart.Validate();
            }
            if (m_ChargeCancel != null)
            {
                m_ChargeCancel.Validate();
            }
            foreach (var chargeLevel in m_ChargedAbilityLevels)
            {
                if (chargeLevel.OnLevelReached != null)
                {
                    chargeLevel.OnLevelReached.Validate();
                }

                if (chargeLevel.OnChargeReleased != null)
                {
                    chargeLevel.OnChargeReleased.Validate();
                }
            }
        }
#endif

        public enum ChargeReleaseConstraint
        {
            // Can charge indefinitly.
            None,
            // Automatically release when the last charge level is reached.
            ReleaseOnMaxChargeReached,
            // Release if charging duration last for the provided duration.
            ReleaseOnTimeout,
            // Cancel the ability if charging duration last for the provided duration.
            CancelOnTimeout,
        }

        [System.Serializable]
        private class ChargeLevelData
        {

            [Tooltip("Cumulative duration (in addition to all previous level treshold) to reach this level.")]
            [SerializeField] private float m_tresholdDuration;
            // Ability to play when releasing
            [SerializeField] private ActionModel m_OnLevelReached;
            [SerializeField] private ActionModel m_OnChargeRelease;


            public float TresholdDuration => m_tresholdDuration;
            public ActionModel OnLevelReached => m_OnLevelReached;
            public ActionModel OnChargeReleased => m_OnChargeRelease;
        }

        /// <summary>
        /// Represents a unified whole of module and settings representing an ability execution.
        /// </summary>
        [System.Serializable]
        public class ActionModel
        {
            [Tooltip("Module responsible for execution when using a execution driver. If not set, Awaitable execution driver will be used.")]
            [SerializeReference, AllowNesting, ShowIf("UsesExecutionDriver"), ReadOnly]
            private AbilityModuleDefinition m_ExecutionDriverModule;

            [Tooltip("Context for Awaitable based execution driver.")]
            [SerializeField, AllowNesting, HideIf("UsesExecutionDriver")]
            private AwaitableExecutionContext m_AwaitableExecutionContext;

            [SerializeField] private AbilityModuleDefinition[] m_Modules;

            [Tooltip("When enabled, the action will happen without affecting the current state of the Ability. " +
                "This means that the ability will not call:" +
                "\n\t- OnAbilityStartExecution" +
                "\n\t- OnAbilityChainOpportunity" +
                "\nThis also meant the ability internal state (ExecutionState) will not change to:" +
                "\n\t- InProgress" +
                "\n\t- ChainOpportunity/Cooldown" +
                "\n\nRecommended for for StartCharge/OnLevelReached.")]
            [SerializeField] private bool m_BackgroundExecution = false;

            public IReadOnlyList<AbilityModuleDefinition> Modules => m_Modules;
            public AbilityModuleDefinition ExecutionDriverModule => m_ExecutionDriverModule;
            public float ExecutionDelay => m_AwaitableExecutionContext.ExecutionDelay;
            public float UpdateDuration => m_AwaitableExecutionContext.UpdateDuration;
            public float ChainOpportunityDuration => m_AwaitableExecutionContext.ChainOpportunityDuration;
            public bool TerminateExecutionOnCompletion => m_AwaitableExecutionContext.TerminateExecutionOnCompletion;
            public bool BackgroundExecution => m_BackgroundExecution;

#if UNITY_EDITOR
            // Used by ShowIf/HideIf Attribute.
            private bool UsesExecutionDriver => m_ExecutionDriverModule != null;

            internal void Validate()
            {
                int executionDriverModuleIndexToRemove = -1;
                m_ExecutionDriverModule = null;

                // Retrieve the first execution driver module from the modules list.
                for (int i = 0; i < m_Modules.Length; i++)
                {
                    if (m_ExecutionDriverModule == null)
                    {
                        if (m_Modules[i] != null && m_Modules[i] is IAbilityExecutionDriverModuleDefinition)
                        {
                            m_ExecutionDriverModule = m_Modules[i];
                            continue;
                        }
                    }
                    // if another execution driver module is found, throw an error.
                    else if (m_Modules[i] is IAbilityExecutionDriverModuleDefinition)
                    {
                        executionDriverModuleIndexToRemove = i;
                        break;
                    }
                }

                if (executionDriverModuleIndexToRemove != -1)
                {
                    Debug.LogWarning($"Multiple execution driver modules found in the action model. Only one execution driver module is allowed." +
                        $"Removing additional driver: {m_Modules[executionDriverModuleIndexToRemove].name}.");
                    m_Modules[executionDriverModuleIndexToRemove] = null;
                }
            }
#endif

            [System.Serializable]
            private class AwaitableExecutionContext
            {
                [Tooltip("Delay between the ability instance's 'Start' and 'Stop'")]
                [Min(0)] public float ExecutionDelay = 0f;
                [Tooltip("Delay between the ability instance's 'Update' and 'Stop'")]
                [Min(0)] public float UpdateDuration = 0.5f;
                [Tooltip("Delay between the ability instance's 'Stop' and ability completion.")]
                [Min(0)] public float ChainOpportunityDuration = 0.25f;
                [Tooltip("When enabled, TerminateExecution() is called after ChainOpportunity duration." +
                    "\nNote: Modules effects are stopped before ChainOpportunity." +
                    "\n\nTerminateExecution() will:" +
                    "\n\t1. Reset current charge level" +
                    "\n\t2. Set next ability command to Default" +
                    "\n\t3. Raise OnAbilityCompleteExecution")]
                public bool TerminateExecutionOnCompletion = true;
            }
        }
    }
}

