using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NobunAtelier
{
    /// <summary>
    /// Base class for all data collections in the NobunAtelier framework.
    /// This class provides the foundation for a data-driven architecture using ScriptableObjects.
    /// It enables designers to create and manage collections of data definitions that can be used
    /// across different systems like factories, audio management, and more.
    /// </summary>
    public abstract class DataCollection : ScriptableObject
    {
        /// <summary>
        /// Gets a random definition from the collection.
        /// </summary>
        /// <returns>A random DataDefinition from the collection, or null if the collection is empty.</returns>
        public abstract DataDefinition GetRandomDefinition();

#if UNITY_EDITOR
        /// <summary>
        /// Gets all data definitions in this collection for editor purposes.
        /// </summary>
        public abstract DataDefinition[] EditorDataDefinitions { get; }

        /// <summary>
        /// Gets the type of definitions stored in this collection.
        /// </summary>
        /// <returns>The Type of the DataDefinition subclass used by this collection.</returns>
        public abstract Type GetDefinitionType();

        /// <summary>
        /// Adds a new definition asset to the collection.
        /// </summary>
        /// <param name="asset">The DataDefinition asset to add.</param>
        public abstract void AddDefinitionAssetToCollection(DataDefinition asset);

        /// <summary>
        /// Creates a new definition in the collection.
        /// </summary>
        /// <param name="duplicateFromLastData">If true, duplicates the last definition's properties.</param>
        public abstract void CreateDefinition(bool duplicateFromLastData = true);

        /// <summary>
        /// Saves the collection and its definitions to disk.
        /// </summary>
        public abstract void SaveCollection();

        /// <summary>
        /// Gets a definition by its name.
        /// </summary>
        /// <param name="name">The name of the definition to find.</param>
        /// <returns>The found DataDefinition, or null if not found.</returns>
        public abstract DataDefinition GetDefinition(string name);

        /// <summary>
        /// Gets a definition by its name, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the definition to find or create.</param>
        /// <returns>The found or newly created DataDefinition.</returns>
        public abstract DataDefinition GetOrCreateDefinition(string name);

        /// <summary>
        /// Deletes a definition from the collection.
        /// </summary>
        /// <param name="index">The index of the definition to delete.</param>
        /// <param name="name">The name of the definition to delete (for validation).</param>
        public abstract void DeleteDefinition(int index, string name);

        /// <summary>
        /// Renames a definition in the collection.
        /// </summary>
        /// <param name="index">The index of the definition to rename.</param>
        /// <param name="name">The new name for the definition.</param>
        public abstract void RenameDefinition(int index, string name);

        /// <summary>
        /// Moves a definition to a new position in the collection.
        /// </summary>
        /// <param name="oldIndex">The current index of the definition.</param>
        /// <param name="newIndex">The new index for the definition.</param>
        public abstract void MoveDefinition(int oldIndex, int newIndex);

        /// <summary>
        /// Forces a definition to be set at a specific index in the collection.
        /// This is useful for editor operations and data migration.
        /// </summary>
        /// <param name="index">The index where to set the definition.</param>
        /// <param name="data">The DataDefinition to set.</param>
        public abstract void ForceSetDataDefinition(int index, DataDefinition data);
#endif
    }

    /// <summary>
    /// Generic base class for data collections that provides type-safe access to definitions.
    /// This class implements the core functionality for managing collections of specific DataDefinition types.
    /// </summary>
    /// <typeparam name="T">The type of DataDefinition stored in this collection. Must inherit from DataDefinition.</typeparam>
    public abstract class DataCollection<T> : DataCollection, IEnumerable<T>
        where T : DataDefinition
    {
        [FormerlySerializedAs("m_dataDefinitions")]
        [SerializeField]
        protected internal List<T> m_DataDefinitions = new List<T>();

        /// <summary>
        /// Gets a read-only list of all definitions in this collection.
        /// </summary>
        public IReadOnlyList<T> Definitions => m_DataDefinitions;

        /// <summary>
        /// Gets the number of definitions in this collection.
        /// </summary>
        public int Count => m_DataDefinitions.Count;

        /// <summary>
        /// Gets the definition at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the definition to get.</param>
        /// <returns>The definition at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when index is less than 0 or greater than or equal to Count.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= m_DataDefinitions.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
                }
                return m_DataDefinitions[index];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return m_DataDefinitions.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets a random definition from the collection.
        /// </summary>
        /// <returns>A random T from the collection, or null if the collection is empty.</returns>
        public override DataDefinition GetRandomDefinition()
        {
            if (m_DataDefinitions.Count == 0)
            {
                return null;
            }

            return m_DataDefinitions[UnityEngine.Random.Range(0, m_DataDefinitions.Count)];
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets all data definitions in this collection for editor purposes.
        /// </summary>
        public override DataDefinition[] EditorDataDefinitions => m_DataDefinitions.ToArray();

        /// <summary>
        /// Gets the type of definitions stored in this collection.
        /// </summary>
        /// <returns>The Type of T used by this collection.</returns>
        public override Type GetDefinitionType()
        {
            return typeof(T);
        }

        /// <summary>
        /// Adds a new definition asset to the collection.
        /// Validates the type and name before adding.
        /// </summary>
        /// <param name="asset">The DataDefinition asset to add.</param>
        public override void AddDefinitionAssetToCollection(DataDefinition asset)
        {
            if (!asset.GetType().IsAssignableFrom(typeof(T)))
            {
                Debug.LogWarning($"Trying to add '{asset.name}'({typeof(T).Name}) to '{this.name}'({this.GetType().Name}), but an the types are not compatibles.", this);
                return;
            }

            foreach (var dataDefinition in m_DataDefinitions)
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

        /// <summary>
        /// Clones an asset and adds it to the collection.
        /// </summary>
        /// <param name="sourceAsset">The source asset to clone.</param>
        /// <param name="name">The name for the new asset.</param>
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

            m_DataDefinitions.Add(newAsset);

            AssetDatabase.AddObjectToAsset(newAsset, this);
            EditorUtility.SetDirty(newAsset);
            AssetDatabase.SaveAssetIfDirty(newAsset);
        }

        /// <summary>
        /// Creates a new definition in the collection.
        /// </summary>
        /// <param name="duplicateFromLastData">If true, duplicates the last definition's properties.</param>
        public override sealed void CreateDefinition(bool duplicateFromLastData = true)
        {
            int containerCount = m_DataDefinitions.Count;

            if (!duplicateFromLastData || containerCount == 0)
            {
                T data = ScriptableObject.CreateInstance<T>();
                data.name = $"{this.name}_data_{containerCount}";
                m_DataDefinitions.Add(data);
                AssetDatabase.AddObjectToAsset(data, this);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }
            else
            {
                var latestData = m_DataDefinitions[containerCount - 1];
                CloneAsset(latestData, $"{latestData.name.Split("_$")[0]}_${GUID.Generate().ToString()}");
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Gets a definition by its name.
        /// </summary>
        /// <param name="name">The name of the definition to find.</param>
        /// <returns>The found T, or null if not found.</returns>
        public override sealed DataDefinition GetDefinition(string name)
        {
            foreach (var dataDefinition in m_DataDefinitions)
            {
                if (dataDefinition.name == name)
                {
                    return dataDefinition;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a definition by its name, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the definition to find or create.</param>
        /// <returns>The found or newly created T.</returns>
        public override sealed DataDefinition GetOrCreateDefinition(string name)
        {
            foreach (var dataDefinition in m_DataDefinitions)
            {
                if (dataDefinition.name == name)
                {
                    return dataDefinition;
                }
            }

            T data = ScriptableObject.CreateInstance<T>();
            data.name = name;
            m_DataDefinitions.Add(data);

            AssetDatabase.AddObjectToAsset(data, this);
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(data);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();

            return data;
        }

        /// <summary>
        /// Deletes a definition from the collection.
        /// </summary>
        /// <param name="index">The index of the definition to delete.</param>
        /// <param name="name">The name of the definition to delete (for validation).</param>
        public override sealed void DeleteDefinition(int index, string name)
        {
            if (m_DataDefinitions.Count <= index || m_DataDefinitions[index].name != name)
            {
                return;
            }

            AssetDatabase.RemoveObjectFromAsset(m_DataDefinitions[index]);
            m_DataDefinitions.RemoveAt(index);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Renames a definition in the collection.
        /// </summary>
        /// <param name="index">The index of the definition to rename.</param>
        /// <param name="name">The new name for the definition.</param>
        public override sealed void RenameDefinition(int index, string name)
        {
            if (m_DataDefinitions.Count <= index)
            {
                return;
            }

            m_DataDefinitions[index].name = name;

            EditorUtility.SetDirty(m_DataDefinitions[index]);
            AssetDatabase.SaveAssetIfDirty(m_DataDefinitions[index]);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Moves a definition to a new position in the collection.
        /// </summary>
        /// <param name="previousIndex">The current index of the definition.</param>
        /// <param name="newIndex">The new index for the definition.</param>
        public override void MoveDefinition(int previousIndex, int newIndex)
        {
            Debug.Assert(previousIndex < m_DataDefinitions.Count);
            Debug.Assert(newIndex < m_DataDefinitions.Count);
            var dataToMove = m_DataDefinitions[previousIndex];
            m_DataDefinitions.RemoveAt(previousIndex);
            m_DataDefinitions.Insert(newIndex, dataToMove);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Saves the collection and its definitions to disk.
        /// </summary>
        public override void SaveCollection()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Forces a definition to be set at a specific index in the collection.
        /// This is useful for editor operations and data migration.
        /// </summary>
        /// <param name="index">The index where to set the definition.</param>
        /// <param name="data">The DataDefinition to set.</param>
        public override void ForceSetDataDefinition(int index, DataDefinition data)
        {
            var typedData = data as T;
            Debug.Assert(typedData != null, $"{this.name}: Trying to assigned {data.name} to the collection, but is not of the right type {typeof(T)}");
            Debug.Assert(index < m_DataDefinitions.Count);

            if (!typedData)
            {
                return;
            }

            m_DataDefinitions[index] = data as T;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
            AssetDatabase.Refresh();
        }

#endif
    }
}