using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// DataDefinition wrapper for <see cref="AbilityActionData"/>.
    /// Use as a shared asset when the same action is referenced by multiple skills.
    /// For one-off actions, inline <see cref="AbilityActionData"/> directly on SkillDefinition.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Action")]
    public class AbilityAction : DataDefinition, ISerializationCallbackReceiver
    {
        [SerializeField] private AbilityActionData m_Data;

#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField] private string m_ValidationMessages;

        internal string ValidationMessages => m_ValidationMessages;
#endif

        public AbilityActionData Data => m_Data;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            m_Data?.DeduplicateInlineEffects(new HashSet<AbilityEffect>());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_Data?.AutoFillDescriptions();
            m_Data?.DeduplicateInlineEffects(new HashSet<AbilityEffect>());

            var sb = new StringBuilder();
            AbilityActionData.Validate(m_Data, "Action", sb);
            m_ValidationMessages = sb.ToString();
        }
#endif
    }
}
