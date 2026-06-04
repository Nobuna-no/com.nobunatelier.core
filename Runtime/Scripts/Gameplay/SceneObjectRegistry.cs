using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Static registry for scene-level objects identified by <see cref="SceneObjectID"/>.
    /// Entries are managed by <see cref="SceneObjectProvider"/> components via OnEnable/OnDisable.
    /// </summary>
    public static class SceneObjectRegistry
    {
        private static readonly Dictionary<SceneObjectID, GameObject> s_Registry = new();

        public static void Register(SceneObjectID id, GameObject gameObject)
        {
            if (id == null)
            {
                Debug.LogWarning("[SceneObjectRegistry] Attempted to register with null ID.", gameObject);
                return;
            }

            if (s_Registry.ContainsKey(id))
            {
                Debug.LogWarning($"[SceneObjectRegistry] ID '{id.name}' already registered. Overwriting.", gameObject);
            }

            s_Registry[id] = gameObject;
        }

        public static void Unregister(SceneObjectID id)
        {
            if (id != null)
            {
                s_Registry.Remove(id);
            }
        }

        public static bool TryGet(SceneObjectID id, out GameObject gameObject)
        {
            if (id != null && s_Registry.TryGetValue(id, out gameObject) && gameObject != null)
            {
                return true;
            }

            gameObject = null;
            return false;
        }

        public static bool TryGetComponent<T>(SceneObjectID id, out T component) where T : Component
        {
            if (TryGet(id, out var gameObject))
            {
                component = gameObject.GetComponent<T>();
                return component != null;
            }

            component = null;
            return false;
        }
    }
}
