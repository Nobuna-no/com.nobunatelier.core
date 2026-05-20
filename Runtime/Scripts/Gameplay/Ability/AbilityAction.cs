using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// ScriptableObject wrapper for <see cref="AbilityActionData"/>.
    /// Use as a shared asset when the same action is referenced by multiple skills.
    /// For one-off actions, inline <see cref="AbilityActionData"/> directly on SkillDefinition.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Action")]
    public class AbilityAction : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private AbilityActionData m_Data;

        public AbilityActionData Data => m_Data;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            m_Data?.DeduplicateInlineEffects(new HashSet<AbilityEffect>());
        }
    }
}
