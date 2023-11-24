using UnityEngine;

namespace NobunAtelier
{
    public class PoolObjectDefinition : DataDefinition
    {
        public PoolableBehaviour PoolableObject => m_poolablePrefab;
        public int ReserveSize => m_reserveSize;

        [SerializeField]
        private PoolableBehaviour m_poolablePrefab;

        [SerializeField]
        private int m_reserveSize = 10;
    }
}