using System;
using NaughtyAttributes;
using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// Determines the lifecycle behavior when an <see cref="EventBinding"/> fires.
    /// </summary>
    public enum BindingAction
    {
        /// <summary>Fire-and-forget: create instance, call Execute, done.</summary>
        Execute,

        /// <summary>Start a duration-bound effect: create instance, call Execute, keep alive for Update calls.</summary>
        Start,

        /// <summary>Stop a running duration-bound effect: find running instance of same effect, call Stop.</summary>
        Stop
    }

    /// <summary>
    /// Maps a <see cref="GameplayEventDefinition"/> to an <see cref="AbilityEffect"/> with per-binding configuration.
    /// Multiple bindings can map to the same event (e.g., Hit1 triggers both DamageArea and Feedback).
    /// Start/Stop pairing uses matching shared <see cref="AbilityEffectAsset"/> references.
    /// </summary>
    [Serializable]
    public class EventBinding
    {
        [Tooltip("The gameplay event that triggers this binding.")]
        [SerializeField] private GameplayEventDefinition m_Event;

        [Tooltip("Lifecycle behavior: Execute (fire-and-forget), Start (keep alive), Stop (end running instance).")]
        [SerializeField] private BindingAction m_Action;

        [Tooltip("Multiplier applied to SkillDefinition.Value for this binding's effect.")]
        [SerializeField] private float m_ValueMultiplier = 1f;

        [Tooltip("Whether the effect targets self or the ability's target.")]
        [SerializeField] private EffectTarget m_Target;

        [Tooltip("Delay in seconds before executing the effect after the event fires.")]
        [Min(0f)]
        [SerializeField] private float m_StartDelay;

        [Tooltip("Position offset relative to the target transform.")]
        [SerializeField] private Vector3 m_PositionOffset;

        [Tooltip("Rotation offset relative to the target transform.")]
        [SerializeField] private Quaternion m_RotationOffset = Quaternion.identity;

        [Tooltip("If true, use a shared AbilityEffectAsset SO. If false, use an inline AbilityEffect.")]
        [SerializeField] private bool m_UseSharedEffect;

        [Tooltip("Shared effect asset (SO reference). Used when UseSharedEffect is true.")]
        [AllowNesting, ShowIf("m_UseSharedEffect")]
        [SerializeField] private AbilityEffectAsset m_SharedEffect;

        [Tooltip("Inline effect definition. Used when UseSharedEffect is false.")]
        [AllowNesting, HideIf("m_UseSharedEffect")]
        [SerializeReference, SubclassSelector] private AbilityEffect m_InlineEffect;

        public GameplayEventDefinition Event => m_Event;
        public BindingAction Action => m_Action;
        public float ValueMultiplier => m_ValueMultiplier;
        public EffectTarget Target => m_Target;
        public float StartDelay => m_StartDelay;
        public Vector3 PositionOffset => m_PositionOffset;
        public Quaternion RotationOffset => m_RotationOffset;
        public bool UseSharedEffect => m_UseSharedEffect;
        public AbilityEffectAsset SharedEffect => m_SharedEffect;
        public AbilityEffect InlineEffect => m_InlineEffect;

        /// <summary>
        /// Resolves the active effect definition based on the UseSharedEffect toggle.
        /// Returns null if no effect is assigned.
        /// </summary>
        public AbilityEffect Resolved => m_UseSharedEffect ? m_SharedEffect?.Definition : m_InlineEffect;
    }
}
