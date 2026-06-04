using System;
using System.Collections.Generic;
using System.Text;
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
    public class SkillDefinition : DataDefinition, ISerializationCallbackReceiver
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
        [HideInInspector]
        [SerializeField] private string m_ValidationMessages;

        internal string ValidationMessages => m_ValidationMessages;

        private bool IsHold => m_Mode == SkillMode.Hold;
        private bool IsCurveOffset => m_Movement == MovementMode.CurveOffset;

        private void OnValidate()
        {
            var sb = new StringBuilder();
            ValidateActionRef(m_DefaultAction, "DefaultAction", sb);

            if (m_Mode == SkillMode.Hold && m_HoldConfig != null)
            {
                ValidateActionRef(m_HoldConfig.HoldStartActionRef, "HoldStartAction", sb);
                ValidateActionRef(m_HoldConfig.HoldCancelActionRef, "HoldCancelAction", sb);

                if (m_HoldConfig.HoldLevels != null)
                {
                    for (int i = 0; i < m_HoldConfig.HoldLevels.Length; i++)
                    {
                        var level = m_HoldConfig.HoldLevels[i];
                        ValidateActionRef(level.OnLevelReachedRef, $"HoldLevel[{i}]/OnLevelReached", sb);
                        ValidateActionRef(level.OnReleasedRef, $"HoldLevel[{i}]/OnReleased", sb);
                    }
                }
            }

            m_ValidationMessages = sb.ToString();
        }

        private static void ValidateActionRef(AbilityActionReference actionRef, string context, StringBuilder sb)
        {
            if (actionRef == null)
                return;

            // Only validate inline data — asset refs have their own OnValidate
            if (actionRef.UseAsset)
                return;

            var data = actionRef.Resolve();
            if (data == null)
                return;

            AbilityActionData.Validate(data, context, sb);
            data.AutoFillDescriptions();
        }
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
            internal AbilityActionReference HoldStartActionRef => m_HoldStartAction;
            internal AbilityActionReference HoldCancelActionRef => m_HoldCancelAction;

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

#if UNITY_EDITOR
            internal AbilityActionReference OnLevelReachedRef => m_OnLevelReached;
            internal AbilityActionReference OnReleasedRef => m_OnReleased;
#endif
        }

        public enum HoldConstraint
        {
            None,
            ReleaseOnMaxChargeReached,
            ReleaseOnTimeout,
            CancelOnTimeout
        }

        // --- ISerializationCallbackReceiver ---

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            var seen = new HashSet<AbilityEffect>();
            m_DefaultAction?.Resolve()?.DeduplicateInlineEffects(seen);

            if (m_Mode == SkillMode.Hold && m_HoldConfig != null)
            {
                m_HoldConfig.HoldStartAction?.DeduplicateInlineEffects(seen);
                m_HoldConfig.HoldCancelAction?.DeduplicateInlineEffects(seen);

                if (m_HoldConfig.HoldLevels != null)
                {
                    foreach (var level in m_HoldConfig.HoldLevels)
                    {
                        level.OnLevelReached?.DeduplicateInlineEffects(seen);
                        level.OnReleased?.DeduplicateInlineEffects(seen);
                    }
                }
            }
        }
    }
}
