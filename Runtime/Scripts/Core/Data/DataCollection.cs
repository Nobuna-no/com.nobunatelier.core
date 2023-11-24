using System.Collections.Generic;
using UnityEngine;
using System;

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

        public abstract Type GetDefinitionType();

        public abstract void AddDefinitionAssetToCollection(DataDefinition asset);

        public abstract void CreateDefinition(bool duplicateFromLastData = true);

        public abstract void SaveCollection();

        public abstract DataDefinition GetDefinition(string name);

        public abstract DataDefinition GetOrCreateDefinition(string name);

        public abstract void DeleteDefinition(int index, string name);

        public abstract void RenameDefinition(int index, string name);

        public abstract void MoveDefinition(int oldIndex, int newIndex);

        public abstract void ForceSetDataDefinition(int index, DataDefinition data);

#endif
    }

    public abstract class DataCollection<T> : DataCollection
        where T : DataDefinition
    {
        [SerializeField]
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

            return m_dataDefinitions[UnityEngine.Random.Range(0, m_dataDefinitions.Count)];
        }

#if UNITY_EDITOR

        public override Type GetDefinitionType()
        {
            return typeof(T);
        }

        public override void AddDefinitionAssetToCollection(DataDefinition asset)
        {
            if (!asset.GetType().IsAssignableFrom(typeof(T)))
            {
                Debug.LogWarning($"Trying to add '{asset.name}'({typeof(T).Name}) to '{this.name}'({this.GetType().Name}), but an the types are not compatibles.", this);
                return;
            }

            foreach (var dataDefinition in m_dataDefinitions)
            {
                if (dataDefinition.name == asset.name)
                {
                    Debug.LogWarning($"Trying to add '{asset.name}'({typeof(T).Name}) to '{this.name}'({this.GetType().Name}), but an asset with the same name already exist.", this);
                    return;
                }
            }

            CloneAsset(asset, asset.name);

            Selection.activeObject = null;
            var assetPath = AssetDatabase.GetAssetPath(asset);
            AssetDatabase.MoveAssetToTrash(assetPath);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
            Selection.activeObject = this;
        }

        private void CloneAsset(DataDefinition sourceAsset, string name)
        {
            T newAsset = ScriptableObject.CreateInstance<T>();
            newAsset.name = name;

            SerializedObject sourceSerializedObject = new SerializedObject(sourceAsset);
            SerializedObject newSerializedObject = new SerializedObject(newAsset);

            SerializedProperty sourceProperty = sourceSerializedObject.GetIterator();

            // Iterate through the properties and copy values
            while (sourceProperty.NextVisible(true))
            {
                if (sourceProperty.name == "m_Script")
                {
                    continue;
                }

                newSerializedObject.CopyFromSerializedProperty(sourceProperty);
            }

            // Apply changes and save the new asset
            newSerializedObject.ApplyModifiedProperties();

            m_dataDefinitions.Add(newAsset);

            AssetDatabase.AddObjectToAsset(newAsset, this);
            EditorUtility.SetDirty(newAsset);
            AssetDatabase.SaveAssetIfDirty(newAsset);
        }

        public override sealed void CreateDefinition(bool duplicateFromLastData = true)
        {
            int containerCount = m_dataDefinitions.Count;

            if (!duplicateFromLastData || containerCount == 0)
            {
                T data = ScriptableObject.CreateInstance<T>();
                data.name = $"{this.name}_data_{containerCount}";
                m_dataDefinitions.Add(data);
                AssetDatabase.AddObjectToAsset(data, this);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }
            else
            {
                var latestData = m_dataDefinitions[containerCount - 1];
                CloneAsset(latestData, $"{latestData.name.Split("_$")[0]}_${GUID.Generate().ToString()}");
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        public override sealed DataDefinition GetDefinition(string name)
        {
            foreach (var dataDefinition in m_dataDefinitions)
            {
                if (dataDefinition.name == name)
                {
                    return dataDefinition;
                }
            }

            return null;
        }

        public override sealed DataDefinition GetOrCreateDefinition(string name)
        {
            foreach (var dataDefinition in m_dataDefinitions)
            {
                if (dataDefinition.name == name)
                {
                    return dataDefinition;
                }
            }

            T data = ScriptableObject.CreateInstance<T>();
            data.name = name;
            m_dataDefinitions.Add(data);

            AssetDatabase.AddObjectToAsset(data, this);
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(data);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();

            return data;
        }

        public override sealed void DeleteDefinition(int index, string name)
        {
            if (m_dataDefinitions.Count <= index || m_dataDefinitions[index].name != name)
            {
                return;
            }

            AssetDatabase.RemoveObjectFromAsset(m_dataDefinitions[index]);
            m_dataDefinitions.RemoveAt(index);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        public override sealed void RenameDefinition(int index, string name)
        {
            if (m_dataDefinitions.Count <= index)
            {
                return;
            }

            m_dataDefinitions[index].name = name;

            EditorUtility.SetDirty(m_dataDefinitions[index]);
            AssetDatabase.SaveAssetIfDirty(m_dataDefinitions[index]);
            AssetDatabase.Refresh();
        }

        public override void MoveDefinition(int previousIndex, int newIndex)
        {
            Debug.Assert(previousIndex < m_dataDefinitions.Count);
            Debug.Assert(newIndex < m_dataDefinitions.Count);
            var dataToMove = m_dataDefinitions[previousIndex];
            m_dataDefinitions.RemoveAt(previousIndex);
            m_dataDefinitions.Insert(newIndex, dataToMove);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        public override void SaveCollection()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        public override void ForceSetDataDefinition(int index, DataDefinition data)
        {
            var typedData = data as T;
            Debug.Assert(typedData != null, $"{this.name}: Trying to assigned {data.name} to the collection, but is not of the right type {typeof(T)}");
            Debug.Assert(index < m_dataDefinitions.Count);

            if (!typedData)
            {
                return;
            }

            m_dataDefinitions[index] = data as T;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

#endif
    }
}