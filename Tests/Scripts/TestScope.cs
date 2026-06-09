using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NobunAtelier.Tests
{
    /// <summary>
    /// Manages temporary UnityEngine.Object lifetimes during tests.
    /// Tracks DataDefinition ScriptableObjects and GameObjects, destroying all on Dispose.
    /// Use in [SetUp]/[TearDown] to prevent resource leaks across tests.
    /// </summary>
    public class TestScope : IDisposable
    {
        private readonly List<Object> m_TrackedObjects = new();

        public T CreateDefinition<T>(string name = null) where T : DataDefinition
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = name ?? typeof(T).Name;
            m_TrackedObjects.Add(instance);
            return instance;
        }

        public GameObject CreateGameObject(string name = null)
        {
            var go = new GameObject(name ?? "TestObject");
            m_TrackedObjects.Add(go);
            return go;
        }

        public void Dispose()
        {
            for (int i = m_TrackedObjects.Count - 1; i >= 0; i--)
            {
                if (m_TrackedObjects[i] != null)
                    Object.DestroyImmediate(m_TrackedObjects[i]);
            }
            m_TrackedObjects.Clear();
        }
    }
}
