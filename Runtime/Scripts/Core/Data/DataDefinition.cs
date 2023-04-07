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
        protected T m_data;
    }
}