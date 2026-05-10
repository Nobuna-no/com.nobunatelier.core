using System;
using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Top-level combat action definition.
    /// Configures value, movement, mode (OneShot/Hold), gameplay tags per state,
    /// and references <see cref="AbilityAction"/>s for execution.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Skill Definition")]
    public class SkillDefinition : DataDefinition
    {
        [Header("Skill")]
        [Tooltip("Base value for this skill (damage, heal amount, etc.). Multiplied by EventBinding.ValueMultiplier.")]
        [SerializeField] private float m_Value;

        [Tooltip("How movement is handled during this skill.")]
        [SerializeField] private MovementMode m_Movement = MovementMode.None;

        [Tooltip("Movement curve applied when MovementMode is CurveOffset.")]
        [AllowNesting, ShowIf("IsCurveOffset")]
        [SerializeField] private AnimationCurve m_MovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Execution mode: OneShot (tap) or Hold (charge/channel).")]
        [SerializeField] private SkillMode m_Mode = SkillMode.OneShot;

        [Header("Default Action")]
        [Tooltip("The AbilityAction executed on tap (OneShot) or early release (Hold).")]
        [SerializeField] private AbilityAction m_DefaultAction;

        [Header("Gameplay Tags")]
        [Tooltip("Tags granted when ability enters Starting state, revoked on state exit.")]
        [SerializeField] private GameplayTagDefinition[] m_OnStartTags;

        [Tooltip("Tags granted when ability enters InProgress state, revoked on state exit.")]
        [SerializeField] private GameplayTagDefinition[] m_OnActiveTags;

        [Tooltip("Tags granted when ability enters Recovery state, revoked on state exit.")]
        [SerializeField] private GameplayTagDefinition[] m_OnRecoveryTags;

        [Header("Hold Configuration")]
        [AllowNesting, ShowIf("IsHold")]
        [SerializeField] private HoldConfig m_HoldConfig;

        // --- Public API ---
        public float Value => m_Value;
        public MovementMode Movement => m_Movement;
        public AnimationCurve MovementCurve => m_MovementCurve;
        public SkillMode Mode => m_Mode;
        public AbilityAction DefaultAction => m_DefaultAction;
        public GameplayTagDefinition[] OnStartTags => m_OnStartTags;
        public GameplayTagDefinition[] OnActiveTags => m_OnActiveTags;
        public GameplayTagDefinition[] OnRecoveryTags => m_OnRecoveryTags;
        public HoldConfig Hold => m_HoldConfig;

#if UNITY_EDITOR
        private bool IsHold => m_Mode == SkillMode.Hold;
        private bool IsCurveOffset => m_Movement == MovementMode.CurveOffset;
#endif

        // --- Enums ---

        public enum MovementMode
        {
            None,
            CurveOffset,
            RootMotion,
            Warping
        }

        public enum SkillMode
        {
            OneShot,
            Hold
        }

        // --- Hold Configuration ---

        [Serializable]
        public class HoldConfig
        {
            [Tooltip("Action played during hold (e.g., charge loop animation).")]
            [SerializeField] private AbilityAction m_HoldStartAction;

            [Tooltip("Charge levels with thresholds and release actions.")]
            [SerializeField] private HoldLevelData[] m_HoldLevels;

            [Tooltip("Action played when hold is cancelled.")]
            [SerializeField] private AbilityAction m_HoldCancelAction;

            [Tooltip("Constraint on charge release behavior.")]
            [SerializeField] private HoldConstraint m_Constraint = HoldConstraint.None;

            [Tooltip("Timeout in seconds (used with timeout-based constraints).")]
            [AllowNesting, ShowIf("HasTimeout")]
            [SerializeField] private float m_Timeout = 3f;

            public AbilityAction HoldStartAction => m_HoldStartAction;
            public HoldLevelData[] HoldLevels => m_HoldLevels;
            public AbilityAction HoldCancelAction => m_HoldCancelAction;
            public HoldConstraint Constraint => m_Constraint;
            public float Timeout => m_Timeout;

#if UNITY_EDITOR
            private bool HasTimeout => m_Constraint == HoldConstraint.ReleaseOnTimeout
                || m_Constraint == HoldConstraint.CancelOnTimeout;
#endif
        }

        [Serializable]
        public class HoldLevelData
        {
            [Tooltip("Cumulative duration in seconds to reach this charge level.")]
            [SerializeField] private float m_ThresholdDuration;

            [Tooltip("Action played when this charge level is reached (e.g., charge flash).")]
            [SerializeField] private AbilityAction m_OnLevelReached;

            [Tooltip("Action played when charge is released at this level.")]
            [SerializeField] private AbilityAction m_OnReleased;

            public float ThresholdDuration => m_ThresholdDuration;
            public AbilityAction OnLevelReached => m_OnLevelReached;
            public AbilityAction OnReleased => m_OnReleased;
        }

        public enum HoldConstraint
        {
            None,
            ReleaseOnMaxChargeReached,
            ReleaseOnTimeout,
            CancelOnTimeout
        }
    }
}
