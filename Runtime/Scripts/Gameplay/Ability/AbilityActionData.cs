using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Core data for an ability action: driver + phase effects + gameplay event groups.
    /// Can be inlined via [SerializeField] or wrapped in an <see cref="AbilityAction"/> SO for reuse.
    /// </summary>
    [Serializable]
    public class AbilityActionData
    {
        [Tooltip("Timing driver that fires GameplayEvents and signals phase transitions.")]
        [SerializeReference, SubclassSelector]
        private IAbilityActionDriver m_Driver;

        [Header("Phase Effects")]
        [Tooltip("Effects fired when entering Start phase (startup VFX, anticipation).")]
        [SerializeField] private List<EffectEntry> m_OnStartEffects;

        [Tooltip("Effects fired when entering Active phase (main effect activation).")]
        [SerializeField] private List<EffectEntry> m_OnActiveEffects;

        [Tooltip("Effects fired when entering Recovery phase (wind-down VFX).")]
        [SerializeField] private List<EffectEntry> m_OnRecoveryEffects;

        [Header("Gameplay Events")]
        [Tooltip("Content event groups. Each maps a GameplayEvent to effects (Hit1 → DamageArea, etc.).")]
        [SerializeField] private List<GameplayEventGroup> m_GameplayEvents;

        public IAbilityActionDriver Driver => m_Driver;
        public IReadOnlyList<EffectEntry> OnStartEffects => m_OnStartEffects;
        public IReadOnlyList<EffectEntry> OnActiveEffects => m_OnActiveEffects;
        public IReadOnlyList<EffectEntry> OnRecoveryEffects => m_OnRecoveryEffects;
        public IReadOnlyList<GameplayEventGroup> GameplayEvents => m_GameplayEvents;

        public GameplayEventDefinition[] GetAvailableEvents()
        {
            return m_Driver?.GetAvailableEvents();
        }

        /// <summary>
        /// Detect and deep-copy duplicate [SerializeReference] inline effects.
        /// Call from ISerializationCallbackReceiver.OnAfterDeserialize on the owning object.
        /// </summary>
        public void DeduplicateInlineEffects(HashSet<AbilityEffect> seen)
        {
            DeduplicateEffects(m_OnStartEffects, seen);
            DeduplicateEffects(m_OnActiveEffects, seen);
            DeduplicateEffects(m_OnRecoveryEffects, seen);

            if (m_GameplayEvents != null)
            {
                foreach (var group in m_GameplayEvents)
                {
                    if (group.Effects is List<EffectEntry> effects)
                        DeduplicateEffects(effects, seen);
                }
            }
        }

        private static void DeduplicateEffects(List<EffectEntry> entries, HashSet<AbilityEffect> seen)
        {
            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                var effect = entry.InlineEffect;
                if (effect == null || entry.UseSharedEffect)
                    continue;

                if (!seen.Add(effect))
                {
                    var json = JsonUtility.ToJson(effect);
                    var clone = (AbilityEffect)Activator.CreateInstance(effect.GetType());
                    JsonUtility.FromJsonOverwrite(json, clone);
                    entry.SetInlineEffect(clone);
                }
            }
        }
    }
}
