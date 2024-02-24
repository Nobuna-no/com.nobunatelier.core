using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    // This PoolManager has been designed to be used with PoolableBehaviour.
    // As each poolable object can define it's own creation and spawned implementation,
    // it's possible to use a single PoolManager to handle any object type.
    // There is also no need to specialized the manager for the spawning method.
    // This is a dynamic pool but is not yet optimized.

    public class PoolManager : Singleton<PoolManager>
    {
        [System.Flags]
        public enum PoolBehaviour
        {
            // Is the pool manager allowed to instantiate new object if there is no available object.
            AllowsLazyInstancing = 1 << 1,

            WarmsLazyInstancing = 1 << 2,
            FillsPoolOnStart = 1 << 3,
            LogDebug = 0x1 << 7,
        }

        // Parent object of where the instantiate objects are placed.
        // In build, it's set to null for optimization.
        [SerializeField] protected Transform m_reserveParent = null;

        [SerializeField]
        private PoolBehaviour m_behaviour =
            PoolBehaviour.AllowsLazyInstancing
            | PoolBehaviour.WarmsLazyInstancing
            | PoolBehaviour.FillsPoolOnStart;

        // This is probably the biggest flaw of this design.
        // Custom DataDefinition<PoolObjectDefinition> cannot be register as collection.
        [SerializeField] private PoolObjectCollection[] m_initialCollections = null;

        // This can also be use by custom PoolObjectDefinition that can't be assigned to the collection.
        [SerializeField] private PoolObjectDefinition[] m_initialDefinitions = null;

        // TODO: Remove this responsibility - need a better options:
        // - object definition.
        // - New behaviour to allow to spawn object in a specific position (PoolSpawner).
        [SerializeField]
        private Vector3 m_spawnRadiusAxis = Vector3.one;

        [SerializeField, Foldout("Debug")]
        private bool m_debugSpawnPositionDisplay = false;

        protected Dictionary<PoolObjectDefinition, List<PoolableBehaviour>> m_objectPoolPerID = new Dictionary<PoolObjectDefinition, List<PoolableBehaviour>>();

        // i.e. PoolManager.RegisterCollection<TileCollection, TileDefinition>(collection);
        public void RegisterCollection<TCollection, TDefinition>(TCollection collection)
            where TCollection : DataCollection<TDefinition>
            where TDefinition : PoolObjectDefinition
        {
            Debug.Assert(collection, this);

            FillReserves(collection.Definitions);
        }

        public void UnregisterCollection<TCollection, TDefinition>(TCollection collection)
            where TCollection : DataCollection<TDefinition>
            where TDefinition : PoolObjectDefinition
        {
            Debug.Assert(collection, this);

            foreach (var definition in collection.Definitions)
            {
                ClearReserve(definition);
            }
        }

        public void RegisterDefinition(PoolObjectDefinition definition)
        {
            Debug.Assert(definition, this);

            FillReserve(definition);
        }

        public void UnregisterDefinition(PoolObjectDefinition definition)
        {
            Debug.Assert(definition, this);

            ClearReserve(definition);
        }

        // From a user perspective, not sure what this is doing....
        public void ResetManager()
        {
            ResetPoolObjects();

            FillInitialReserves();

            OnPoolManagerReset();
        }

        private void ResetPoolObjects()
        {
            foreach (var key in m_objectPoolPerID.Keys)
            {
                foreach (var val in m_objectPoolPerID[key])
                {
                    val.ResetObject();
                }
            }
        }

        // Called once all the object has been reset.
        protected virtual void OnPoolManagerReset()
        { }

        public PoolableBehaviour SpawnObject(PoolObjectDefinition id, Vector3 position)
        {
            if (!m_objectPoolPerID.ContainsKey(id))
            {
                FillReserve(id);

                if ((m_behaviour & PoolBehaviour.WarmsLazyInstancing) != 0)
                {
                    Debug.LogWarning($"{this}[WARM]: Lazy instantiate of object: {id}.", this);
                }
            }

            // Could be optimized for speed
            // In the future, might be nice to provide a search method injection per object
            // This way, high frequency life cycle object can spend a bit more memory to get
            // a faster search (log(n) instead of n, using a second map to monitor inactive objects).
            PoolableBehaviour target = m_objectPoolPerID[id].Find((obj) => { return !obj.IsActive; });

            if (target == null)
            {
                if ((m_behaviour & PoolBehaviour.AllowsLazyInstancing) == 0)
                {
                    Debug.LogWarning($"Cannot force instantiate, object of id: {id}. Skipped...");
                    return null;
                }

                if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
                {
                    Debug.Log($"Instantiating new batch of {id}");
                }

                m_objectPoolPerID[id].AddRange(InstantiateBatch(m_objectPoolPerID[id][0], m_objectPoolPerID[id].Count + id.ReserveGrowCount));
                return SpawnObject(id, position);
            }

            if (m_debugSpawnPositionDisplay)
            {
                Debug.DrawLine(m_reserveParent.position, position, Color.yellow, 3f);
            }

            target.Position = position;
            target.IsActive = true;
            //OnObjectSpawned(target);
            return target;
        }

        public PoolableBehaviour[] SpawnObjects(PoolObjectDefinition id, Vector3 location, float radius, int count)
        {
            PoolableBehaviour[] objects = new PoolableBehaviour[count];
            for (int i = 0; i < count; ++i) 
            {
                objects[i] = SpawnObject(id, GetSpawnPointInRadius(location, radius));
            }

            return objects;
        }

        public T[] SpawnObjects<T>(PoolObjectDefinition id, Vector3 location, float radius, int count)
            where T : PoolableBehaviour
        {
            T[] objects = new T[count];
            for (int i = 0; i < count; ++i)
            {
                objects[i] = SpawnObject(id, GetSpawnPointInRadius(location, radius)) as T;
            }

            return objects;
        }

        protected Vector3 GetSpawnPointInRadius(Vector3 location, float radius)
        {
            Vector3 circlePos = Random.insideUnitSphere * radius;
            return new Vector3(m_spawnRadiusAxis.x * circlePos.x, m_spawnRadiusAxis.y * circlePos.y,
                m_spawnRadiusAxis.z * circlePos.z) + location;
        }

        private PoolableBehaviour[] InstantiateBatch(PoolableBehaviour prefab, int count)
        {
            PoolableBehaviour[] out_array = new PoolableBehaviour[count];

            for (int i = 0; i < count; ++i)
            {
                out_array[i] = Instantiate(prefab.gameObject, Vector3.zero, Quaternion.identity, m_reserveParent).GetComponent<PoolableBehaviour>();
                out_array[i].ResetObject();
            }

            return out_array;
        }

        // Generate the pool of object of the initial definitions.
        private void FillInitialReserves()
        {
            foreach (var collection in m_initialCollections)
            {
                FillReserves(collection.Definitions);
            }

            FillReserves(m_initialDefinitions);
        }

        private void FillReserves(IReadOnlyList<PoolObjectDefinition> definitions)
        {
            foreach (var def in definitions)
            {
                FillReserve(def);
            }
        }

        private void FillReserve(PoolObjectDefinition def)
        {
            PoolObjectDefinition workingObject = def;
            if (!m_objectPoolPerID.ContainsKey(workingObject))
            {
                m_objectPoolPerID.Add(workingObject, new List<PoolableBehaviour>(def.ReserveSize));
            }

            // it's ok if we have more object.
            int reserveCountTarget = def.ReserveSize - m_objectPoolPerID[workingObject].Count;
            if (reserveCountTarget > 0)
            {
                m_objectPoolPerID[workingObject].AddRange(InstantiateBatch(def.PoolableObject, reserveCountTarget));
            }
        }

        private void ClearReserve<TDefinition>(TDefinition definition) where TDefinition : PoolObjectDefinition
        {
            if (m_objectPoolPerID.ContainsKey(definition))
            {
                var list = m_objectPoolPerID[definition];
                foreach (var obj in list)
                {
                    Destroy(obj.gameObject);
                }
                m_objectPoolPerID[definition].Clear();
                m_objectPoolPerID.Remove(definition);
            }
        }

        protected virtual void Start()
        {
            if ((m_behaviour & PoolBehaviour.FillsPoolOnStart) != 0)
            {
                ResetManager();
            }
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void SpawnOneElementOfEachDefinition()
        {
            if (m_initialDefinitions.Length == 0)
            {
                return;
            }

            foreach (var def in m_initialDefinitions)
            {
                SpawnObjects(def, transform.position, 1, 1);
            }
        }

#endif
    }
}