//using NaughtyAttributes;
//using System.Collections.Generic;
//using UnityEngine;
//using NobunAtelier;

//public class PoolManagerV2 : MonoBehaviour
//{
//    [System.Serializable]
//    protected class PoolObjectConfig
//    {
//        public PoolableBehaviour poolObject;

//        [Tooltip("Size of the initial reserve. Number of inactive object to instantiate by default.")]
//        public int reserveSize;
//    }

//    public static PoolManagerV2 Instance => m_instance;
//    protected static PoolManagerV2 m_instance = null;

//    // Parent object of where the instantiate objects are placed.
//    [SerializeField]
//    protected Transform m_reserveParent = null;

//    [SerializeField]
//    private bool m_canForceInstantiateInEmergency = true;

//    [SerializeField]
//    private bool m_resetPoolOnStart = true;

//    [SerializeField]
//    protected PoolObjectConfig[] m_objectsDefinition = null;

//    [SerializeField]
//    private Vector3 m_spawnRadiusAxis = Vector3.one;

//    [SerializeField, Foldout("Debug")]
//    private bool m_debugSpawnPositionDisplay = false;

//    // protected List<IPoolableObject> m_pool = new List<IPoolableObject>();
//    protected Dictionary<PoolObjectDefinition, List<PoolableBehaviour>> m_objectPoolPerID = new Dictionary<PoolObjectDefinition, List<PoolableBehaviour>>();

//    public void ResetManager()
//    {
//        foreach (var key in m_objectPoolPerID.Keys)
//        {
//            foreach (var val in m_objectPoolPerID[key])
//            {
//                val.ResetObject();
//            }
//        }

//        FillReserves();

//        OnPoolManagerReset();
//    }

//    // Called once all the object has been reset.
//    protected virtual void OnPoolManagerReset()
//    { }

//    // Useful to initialize the new object and bind method to IPoolableObject.onActivation for instance.
//    protected virtual void OnObjectCreation(PoolableBehaviour obj)
//    { }

//    protected virtual void OnObjectSpawned(PoolableBehaviour obj)
//    { }

//    public PoolableBehaviour SpawnObject(PoolObjectDefinition id, Vector3 position)
//    {
//        if (!m_objectPoolPerID.ContainsKey(id))
//        {
//            Debug.LogWarning($"Trying to instantiate unknown object of id: {id}. Skipped...");
//            return null;
//        }

//        PoolableBehaviour target = m_objectPoolPerID[id].Find((obj) => { return !obj.IsActive; });

//        if (target == null)
//        {
//            Debug.Log($"Pool overflow for object id: {id}!");

//            if (m_canForceInstantiateInEmergency)
//            {
//                Debug.LogWarning("Instantiating new batch in emergency!");
//                m_objectPoolPerID[id].AddRange(InstantiateBatch(m_objectPoolPerID[id][0], m_objectPoolPerID[id].Count / 2));
//                return SpawnObject(id, position);
//            }

//            Debug.LogWarning($"Cannot force instantiate, object of id: {id}. Skipped...");
//            return null;
//        }

//        if (m_debugSpawnPositionDisplay)
//        {
//            Debug.DrawLine(m_reserveParent.position, position, Color.yellow, 3f);
//        }

//        target.Position = position;
//        target.IsActive = true;
//        OnObjectSpawned(target);
//        return target;
//    }

//    public void SpawnObject(PoolObjectDefinition id, Vector3 location, float radius, int count)
//    {
//        for (int i = 0; i < count; ++i)
//        {
//            SpawnObject(id, GetSpawnPointInRadius(location, radius));
//        }
//    }

//    protected Vector3 GetSpawnPointInRadius(Vector3 location, float radius)
//    {
//        Vector3 circlePos = Random.insideUnitSphere * radius;
//        return new Vector3(m_spawnRadiusAxis.x * circlePos.x, m_spawnRadiusAxis.y * circlePos.y,
//            m_spawnRadiusAxis.z * circlePos.z) + location;
//    }

//    private PoolableBehaviour[] InstantiateBatch(PoolableBehaviour prefab, int count)
//    {
//        PoolableBehaviour[] out_array = new PoolableBehaviour[count];

//        for (int i = 0; i < count; ++i)
//        {
//            out_array[i] = Instantiate(prefab.gameObject, Vector3.zero, Quaternion.identity, m_reserveParent).GetComponent<PoolableBehaviour>();
//            out_array[i].ResetObject();
//            OnObjectCreation(out_array[i]);
//        }

//        return out_array;
//    }

//    private void FillReserves()
//    {
//        foreach (var def in m_objectsDefinition)
//        {
//            PoolObjectDefinition workingID = def;
//            if (!m_objectPoolPerID.ContainsKey(workingID))
//            {
//                m_objectPoolPerID.Add(workingID, new List<PoolableBehaviour>(def.reserveSize));
//            }

//            // it's ok if we have more object.
//            int reserveCountTarget = def.reserveSize - m_objectPoolPerID[workingID].Count;
//            if (reserveCountTarget > 0)
//            {
//                m_objectPoolPerID[workingID].AddRange(InstantiateBatch(def.poolObject, reserveCountTarget));
//            }
//        }
//    }

//    protected virtual void Start()
//    {
//        if (m_resetPoolOnStart)
//        {
//            ResetManager();
//        }
//    }
//}