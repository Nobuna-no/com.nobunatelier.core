using System;
using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// One effect within a <see cref="GameplayEventGroup"/> or a phase effect list.
    /// Defines what effect to execute and how (target, value scaling).
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

        [Tooltip("Multiplier applied to SkillDefinition.Value for this effect.")]
        [SerializeField] private float m_ValueMultiplier = 1f;

        public float ValueMultiplier => m_ValueMultiplier;
        public EffectTarget Target => m_Target;
        public AbilityEffectReference Effect => m_Effect;

        public AbilityEffect Resolved => m_Effect?.Resolve();
        public bool UseSharedEffect => m_Effect?.UseAsset ?? false;
        public AbilityEffect InlineEffect => m_Effect?.InlineData;

        internal void SetInlineEffect(AbilityEffect effect) => m_Effect?.SetInlineData(effect);

#if UNITY_EDITOR
        internal void AutoFillDescription()
        {
            if (!string.IsNullOrEmpty(m_Description))
                return;

            var effect = m_Effect?.Resolve();
            if (effect == null)
                return;

            m_Description = $"[{m_Target}] {FormatEffectName(effect.GetType().Name)}";
        }

        private static string FormatEffectName(string typeName)
        {
            if (typeName.EndsWith("AbilityEffect"))
                return typeName[..^"AbilityEffect".Length];
            if (typeName.EndsWith("Effect"))
                return typeName[..^"Effect".Length];
            return typeName;
        }
#endif
    }
}
