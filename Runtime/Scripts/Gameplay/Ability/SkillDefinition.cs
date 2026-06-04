using System;
using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Top-level combat action definition.
    /// Configures value, movement, mode (OneShot/Hold), gameplay tags per state,
    /// and references <see cref="AbilityActionData"/> for execution (inline or via <see cref="AbilityAction"/> asset).
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Skill Definition")]
    public class SkillDefinition : DataDefinition
    {
        [Header("Skill")]
        [Tooltip("Base value for this skill (damage, heal amount, etc.). Multiplied by EffectEntry.ValueMultiplier.")]
        [SerializeField] private float m_Value = 1f;

        [Tooltip("How movement is handled during this skill.")]
        [SerializeField] private MovementMode m_Movement = MovementMode.None;

        [Tooltip("Movement curve applied when MovementMode is CurveOffset.")]
        [AllowNesting, ShowIf("IsCurveOffset")]
        [SerializeField] private AnimationCurve m_MovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Execution mode: OneShot (tap) or Hold (charge/channel).")]
        [SerializeField] private SkillMode m_Mode = SkillMode.OneShot;

        [Header("Default Action")]
        [SerializeField] private AbilityActionReference m_DefaultAction;

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
        public AbilityActionData DefaultAction => m_DefaultAction?.Resolve();
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
            [SerializeField] private AbilityActionReference m_HoldStartAction;

            [Tooltip("Charge levels with thresholds and release actions.")]
            [SerializeField] private HoldLevelData[] m_HoldLevels;

            [Tooltip("Action played when hold is cancelled.")]
            [SerializeField] private AbilityActionReference m_HoldCancelAction;

            [Tooltip("Constraint on charge release behavior.")]
            [SerializeField] private HoldConstraint m_Constraint = HoldConstraint.None;

            [Tooltip("Timeout in seconds (used with timeout-based constraints).")]
            [AllowNesting, ShowIf("HasTimeout")]
            [SerializeField] private float m_Timeout = 3f;

            public AbilityActionData HoldStartAction => m_HoldStartAction?.Resolve();
            public HoldLevelData[] HoldLevels => m_HoldLevels;
            public AbilityActionData HoldCancelAction => m_HoldCancelAction?.Resolve();
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
            [SerializeField] private AbilityActionReference m_OnLevelReached;

            [Tooltip("Action played when charge is released at this level.")]
            [SerializeField] private AbilityActionReference m_OnReleased;

            public float ThresholdDuration => m_ThresholdDuration;
            public AbilityActionData OnLevelReached => m_OnLevelReached?.Resolve();
            public AbilityActionData OnReleased => m_OnReleased?.Resolve();
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
