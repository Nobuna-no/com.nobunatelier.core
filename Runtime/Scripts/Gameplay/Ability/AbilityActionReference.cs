using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Dual-slot reference: inline <see cref="AbilityActionData"/> or shared <see cref="AbilityAction"/> asset.
    /// </summary>
    [Serializable]
    public class AbilityActionReference : DataReference<AbilityActionData, AbilityAction>
    {
        [SerializeField] private AbilityActionData m_InlineData;

        public AbilityActionData Resolve() => UseAsset ? GetDataFromAsset(Asset) : m_InlineData;
        public AbilityActionData InlineData => m_InlineData;

        protected override AbilityActionData GetDataFromAsset(AbilityAction asset)
            => asset != null ? asset.Data : null;
    }
}
