using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

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
    [SerializeField] private bool m_canChainOnSelf = true;

    [Header("Charge")]
    [SerializeField] private bool m_canBeCharged = false;
    [Tooltip("If released before the first charge level is reached, play normal modules.")]
    [SerializeField, AllowNesting, ShowIf("m_canBeCharged")]
    private bool m_playAbilityOnEarlyChargeRelease = true;

    [SerializeField, AllowNesting, ShowIf("HasTimeoutMode")]
    private float m_chargeTimeout = 3f;

    [SerializeField, AllowNesting, ShowIf("m_canBeCharged")]
    private bool m_cancelAbilityChargeOnEarlyChargeRelease = true;

    [SerializeField, AllowNesting, ShowIf("m_canBeCharged")]
    private ChargeReleaseConstraint m_chargeConstraint = ChargeReleaseConstraint.None;

    [SerializeField, AllowNesting, ShowIf("m_canBeCharged")]
    private ActionModel m_ChargeStart;

    [SerializeField, AllowNesting, ShowIf("m_cancelAbilityChargeOnEarlyChargeRelease")]
    public ActionModel m_ChargeCancel;

    [SerializeField, AllowNesting, ShowIf("m_canBeCharged")]
    private ChargeLevelData[] m_chargedAbilityLevels;

#if UNITY_EDITOR
    private bool HasTimeoutMode => m_canBeCharged
        && (m_chargeConstraint == ChargeReleaseConstraint.ReleaseOnTimeout
        || m_chargeConstraint == ChargeReleaseConstraint.CancelOnTimeout);
    private bool DoesCancelTimeout => m_canBeCharged
        && m_chargeConstraint == ChargeReleaseConstraint.CancelOnTimeout;
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

        [Tooltip("Delay between the ability instance's 'Initiate' and 'Start'")]
        [SerializeField, AllowNesting, HideIf("HasProcessor"), Min(0)]
        private float m_ExecutionDelay = 0.0f;

        [Tooltip("Delay between the ability instance's 'Start' and 'Stop'")]
        [SerializeField, AllowNesting, HideIf("HasProcessor"), Min(0)]
        private float m_UpdateDuration = 0.5f;

        [Tooltip("Delay between the ability instance's 'Stop' and ability completion.")]
        [SerializeField, AllowNesting, HideIf("HasProcessor"), Min(0)]
        private float m_ChainOpportunityDuration = 0.25f;

        [Tooltip("When enabled, TerminateExecution() is called after ChainOpportunity duration." +
            "Note: Modules effects are stopped before ChainOpportunity." +
            "\nTerminateExecution:" +
            "\n\t1. Reset current charge level" +
            "\n\t2. Set next ability command to Default" +
            "\n\t3. Raise OnAbilityCompleteExecution")]
        [SerializeField, AllowNesting, HideIf("HasProcessor")]
        private bool m_TerminateExecutionOnCompletion = true;

        public IReadOnlyList<AbilityModuleDefinition> Modules => m_Modules;
        public float ExecutionDelay => m_ExecutionDelay;
        public float UpdateDuration => m_UpdateDuration;
        public float ChainOpportunityDuration => m_ChainOpportunityDuration;
        public bool TerminateExecutionOnCompletion => m_TerminateExecutionOnCompletion;
        public bool BackgroundExecution => m_BackgroundExecution;

#if UNITY_EDITOR
        // Used by ShowIf Attribute.
        // Kinda dirty, but good enough for now.
        private bool HasProcessor
        {
            get
            {
                if (m_Modules == null || m_Modules.Length == 0)
                {
                    return false;
                }

                foreach (var module in m_Modules)
                {
                    if (module == null || !module.IsInstanceAbilityProcessor)
                    {
                        continue;
                    }

                    return true;
                }
                return false;
            }
        }
#endif
    }
}

