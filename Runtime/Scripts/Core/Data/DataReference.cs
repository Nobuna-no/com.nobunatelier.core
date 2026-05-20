using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Generic dual-slot reference: inline data or shared ScriptableObject asset.
    /// Subclass with concrete types and add a Resolve() method.
    /// Follows AssetOrInline drawer convention (m_UseAsset, m_Asset, m_InlineData).
    /// </summary>
    /// <typeparam name="TData">The inline data type.</typeparam>
    /// <typeparam name="TAsset">The ScriptableObject asset type.</typeparam>
    [Serializable]
    public abstract class DataReference<TData, TAsset>
        where TAsset : ScriptableObject
    {
        [SerializeField, HideInInspector] private bool m_UseAsset;
        [SerializeField] private TAsset m_Asset;

        public bool UseAsset => m_UseAsset;
        public TAsset Asset => m_Asset;

        /// <summary>
        /// Extract inline data from the asset. Implemented by concrete subclass
        /// since the asset's data accessor varies per type.
        /// </summary>
        protected abstract TData GetDataFromAsset(TAsset asset);
    }
}
