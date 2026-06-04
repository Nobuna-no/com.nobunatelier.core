using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Dual-slot reference: inline <see cref="AbilityEffect"/> or shared <see cref="AbilityEffectDefinition"/>.
    /// </summary>
    [Serializable]
    public class AbilityEffectReference : DataReference<AbilityEffect, AbilityEffectDefinition>
    {
        [SerializeReference, SubclassSelector] private AbilityEffect m_InlineData;

        public AbilityEffect Resolve() => UseAsset ? GetDataFromAsset(Asset) : m_InlineData;
        public AbilityEffect InlineData => m_InlineData;

        internal void SetInlineData(AbilityEffect effect) => m_InlineData = effect;

        protected override AbilityEffect GetDataFromAsset(AbilityEffectDefinition definition)
            => definition != null ? definition.Definition : null;
    }
}
