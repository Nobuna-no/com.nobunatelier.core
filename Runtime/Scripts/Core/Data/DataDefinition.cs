using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    /// <summary>
    /// Base class for all data definitions. This class is used to define the data structure for a specific type of data.
    /// </summary>
    public abstract class DataDefinition : ScriptableObject
    {
    }

    /// <summary>
    /// Generic base class for data definitions.
    /// This class is used to define the data structure for a specific type of data.
    /// </summary>
    public abstract class DataDefinition<T> : DataDefinition
    {
        [FormerlySerializedAs("m_data")]
        [SerializeField]
        private T m_Data;

        public T Data => m_Data;
    }

    /// <summary>
    /// Base class for all data instances. This class can be use to separate logic from data.
    /// </summary>
    public abstract class DataInstance<T>
        where T : DataDefinition
    {
        private T m_Data;
        public T Data => m_Data;

        public DataInstance(T data)
        {
            m_Data = data;
        }
    }
}