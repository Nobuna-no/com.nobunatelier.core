using UnityEngine;

namespace NobunAtelier
{
    public abstract class DataDefinition : ScriptableObject
    {
    }

    public abstract class DataDefinition<T> : DataDefinition
    {
        public T Data => m_data;

        [SerializeField]
        private T m_data;
    }
}