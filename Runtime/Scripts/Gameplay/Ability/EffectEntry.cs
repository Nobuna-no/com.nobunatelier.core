using System;
using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// Determines the lifecycle behavior when an effect entry fires.
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
    /// One effect within a <see cref="GameplayEventGroup"/> or a phase effect list.
    /// Defines what effect to execute and how (action, target, value scaling).
    /// </summary>
    [Serializable]
    public class EffectEntry
    {
#if UNITY_EDITOR
        [Tooltip("Editor only")]
        [SerializeField] private string m_Description;
#endif
        [Tooltip("Effect definition (inline or shared asset).")]
        [SerializeField] private AbilityEffectReference m_Effect;
        [Tooltip("Whether the effect targets self or the ability's target.")]
        [SerializeField] private EffectTarget m_Target;
        
        [Tooltip("Lifecycle behavior: Execute (fire-and-forget), Start (keep alive), Stop (end running instance).")]
        [SerializeField] private BindingAction m_Action;

        [Tooltip("Multiplier applied to SkillDefinition.Value for this effect.")]
        [SerializeField] private float m_ValueMultiplier = 1f;

        public BindingAction Action => m_Action;
        public float ValueMultiplier => m_ValueMultiplier;
        public EffectTarget Target => m_Target;
        public AbilityEffectReference Effect => m_Effect;

        public AbilityEffect Resolved => m_Effect?.Resolve();
        public bool UseSharedEffect => m_Effect?.UseAsset ?? false;
        public AbilityEffect InlineEffect => m_Effect?.InlineData;

        internal void SetInlineEffect(AbilityEffect effect) => m_Effect?.SetInlineData(effect);
    }
}
