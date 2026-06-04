using System;
using System.Collections.Generic;
using System.Text;
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

#if UNITY_EDITOR
        internal static void Validate(AbilityActionData data, string context, StringBuilder sb)
        {
            if (data == null)
                return;

            if (data.m_GameplayEvents != null)
            {
                var seenEvents = new HashSet<GameplayEventDefinition>();
                for (int i = 0; i < data.m_GameplayEvents.Count; i++)
                {
                    var group = data.m_GameplayEvents[i];
                    if (group.Event == null)
                    {
                        sb.AppendLine($"{context}: GameplayEvent[{i}] has null event.");
                        continue;
                    }

                    if (!seenEvents.Add(group.Event))
                    {
                        sb.AppendLine($"{context}: Duplicate GameplayEvent '{group.Event.name}' at index {i}.");
                    }
                }
            }

            ValidateEffectList(data.m_OnStartEffects, $"{context}/OnStart", sb);
            ValidateEffectList(data.m_OnActiveEffects, $"{context}/OnActive", sb);
            ValidateEffectList(data.m_OnRecoveryEffects, $"{context}/OnRecovery", sb);

            if (data.m_GameplayEvents != null)
            {
                for (int i = 0; i < data.m_GameplayEvents.Count; i++)
                {
                    var group = data.m_GameplayEvents[i];
                    var eventName = group.Event != null ? group.Event.name : $"[{i}]";
                    ValidateEffectList(group.Effects as List<EffectEntry>, $"{context}/{eventName}", sb);
                }
            }
        }

        private static void ValidateEffectList(List<EffectEntry> entries, string context, StringBuilder sb)
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Resolved == null)
                    sb.AppendLine($"{context}: EffectEntry[{i}] has null effect.");
            }
        }

        internal void AutoFillDescriptions()
        {
            AutoFillList(m_OnStartEffects);
            AutoFillList(m_OnActiveEffects);
            AutoFillList(m_OnRecoveryEffects);

            if (m_GameplayEvents != null)
            {
                foreach (var group in m_GameplayEvents)
                {
                    if (group.Effects is List<EffectEntry> list)
                        AutoFillList(list);
                }
            }
        }

        private static void AutoFillList(List<EffectEntry> entries)
        {
            if (entries == null)
                return;

            foreach (var entry in entries)
                entry.AutoFillDescription();
        }
#endif

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
