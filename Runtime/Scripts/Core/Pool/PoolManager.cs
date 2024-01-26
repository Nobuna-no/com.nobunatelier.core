using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        // Parent object of where the instantiate objects are placed.
        [SerializeField]
        protected Transform m_reserveParent = null;

        [SerializeField]
        private bool m_canForceInstantiateInEmergency = true;

        [SerializeField]
        private bool m_resetPoolOnStart = true;

        [SerializeField]
        protected PoolObjectDefinition[] m_objectsDefinition = null;

        [SerializeField]
        private Vector3 m_spawnRadiusAxis = Vector3.one;

        [SerializeField, Foldout("Debug")]
        private bool m_debugSpawnPositionDisplay = false;

        protected Dictionary<PoolObjectDefinition, List<PoolableBehaviour>> m_objectPoolPerID = new Dictionary<PoolObjectDefinition, List<PoolableBehaviour>>();

        public void ResetManager()
        {
            foreach (var key in m_objectPoolPerID.Keys)
            {
                foreach (var val in m_objectPoolPerID[key])
                {
                    val.ResetObject();
                }
            }

            FillReserves();

            OnPoolManagerReset();
        }

        // Called once all the object has been reset.
        protected virtual void OnPoolManagerReset()
        { }

        // Useful to initialize the new object and bind method to IPoolableObject.onActivation for instance.
        protected virtual void OnObjectCreation(PoolableBehaviour obj)
        { }

        protected virtual void OnObjectSpawned(PoolableBehaviour obj)
        { }

        public PoolableBehaviour SpawnObject(PoolObjectDefinition id, Vector3 position)
        {
            if (!m_objectPoolPerID.ContainsKey(id))
            {
                Debug.LogWarning($"Trying to instantiate unknown object of id: {id}. Skipped...");
                return null;
            }

            PoolableBehaviour target = m_objectPoolPerID[id].Find((obj) => { return !obj.IsActive; });

            if (target == null)
            {
                if (m_canForceInstantiateInEmergency)
                {
                    Debug.Log($"Instantiating new batch of {id}");
                    m_objectPoolPerID[id].AddRange(InstantiateBatch(m_objectPoolPerID[id][0], m_objectPoolPerID[id].Count + id.ReserveGrowCount));
                    return SpawnObject(id, position);
                }

                Debug.LogWarning($"Cannot force instantiate, object of id: {id}. Skipped...");
                return null;
            }

            if (m_debugSpawnPositionDisplay)
            {
                Debug.DrawLine(m_reserveParent.position, position, Color.yellow, 3f);
            }

            target.Position = position;
            target.IsActive = true;
            OnObjectSpawned(target);
            return target;
        }

        public void SpawnObject(PoolObjectDefinition id, Vector3 location, float radius, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                SpawnObject(id, GetSpawnPointInRadius(location, radius));
            }
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
                OnObjectCreation(out_array[i]);
            }

            return out_array;
        }

        private void FillReserves()
        {
            foreach (var def in m_objectsDefinition)
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
        }

        private void Awake()
        {
            Instance = this;
        }

        protected virtual void Start()
        {
            if (m_resetPoolOnStart)
            {
                ResetManager();
            }
        }

#if UNITY_EDITOR
        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void SpawnOneElementOfEachDefinition()
        {
            if (m_objectsDefinition.Length == 0)
            {
                return;
            }

            foreach (var def in m_objectsDefinition)
            {
                SpawnObject(def, transform.position, 1, 1);
            }
        }
#endif
    }
}