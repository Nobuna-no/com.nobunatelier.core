using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace NobunAtelier
{
    public abstract class DataCollection : ScriptableObject
    {
        public abstract DataDefinition[] DataDefinitions { get; }

        public abstract DataDefinition GetRandomDefinition();

#if UNITY_EDITOR

        public abstract void CreateDefinition();

        public abstract void DeleteDefinition(int index, string name);

        public abstract void RenameDefinition(int index, string name);

#endif
    }

    public abstract class DataCollection<T> : DataCollection
        where T : DataDefinition
    {
        [SerializeField, HideInInspector]
        protected List<T> m_dataDefinitions = new List<T>();

        public override DataDefinition[] DataDefinitions => m_dataDefinitions.ToArray();

        public IReadOnlyList<T> GetData()
        {
            return m_dataDefinitions;
        }

        public override DataDefinition GetRandomDefinition()
        {
            if (m_dataDefinitions.Count == 0)
            {
                return null;
            }

            return m_dataDefinitions[Random.Range(0, m_dataDefinitions.Count)];
        }

#if UNITY_EDITOR

        public override sealed void CreateDefinition()
        {
            int containerCount = m_dataDefinitions.Count;

            T data = ScriptableObject.CreateInstance<T>();
            data.name = $"{this.name}_data_{containerCount}";
            m_dataDefinitions.Add(data);

            AssetDatabase.AddObjectToAsset(data, this);
            AssetDatabase.SaveAssets();
        }

        public override sealed void DeleteDefinition(int index, string name)
        {
            if (m_dataDefinitions.Count <= index || m_dataDefinitions[index].name != name)
            {
                return;
            }

            AssetDatabase.RemoveObjectFromAsset(m_dataDefinitions[index]);
            m_dataDefinitions.RemoveAt(index);

            AssetDatabase.SaveAssets();
        }

        public override sealed void RenameDefinition(int index, string name)
        {
            if (m_dataDefinitions.Count <= index)
            {
                return;
            }

            m_dataDefinitions[index].name = name;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

#endif
    }
}