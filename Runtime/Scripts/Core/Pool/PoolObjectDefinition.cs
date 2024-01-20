using UnityEngine;

namespace NobunAtelier
{
    public class PoolObjectDefinition : DataDefinition
    {
        public PoolableBehaviour PoolableObject => m_poolablePrefab;
        public int ReserveSize => m_reserveSize;
        public int ReserveGrowCount => m_reserveGrowCount;

        [SerializeField]
        private PoolableBehaviour m_poolablePrefab;

        [SerializeField]
        private int m_reserveSize = 10;
        [SerializeField, Tooltip("By how much should the pool grow for this object.")]
        private int m_reserveGrowCount = 10;
    }
}