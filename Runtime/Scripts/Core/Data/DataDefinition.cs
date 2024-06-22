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

    public abstract class DataInstance<T>
        where T : DataDefinition
    {
        private T m_data;
        public T Data => m_data;

        public DataInstance(T data)
        {
            m_data = data;
        }
    }
}