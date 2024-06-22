using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [System.Serializable]
    private class SpawnDefinition
    {
        public GameObject Prefab;

        [NaughtyAttributes.MinMaxSlider(0, 20)]
        public Vector2Int InRangeSpawnCount = new Vector2Int(1, 10);

        public Color PreviewColor = Color.red;

        [Min(0.01f)]
        public float PreviewSize = 0.5f;
    }

    [Header("Radius Spawner")]
    [NaughtyAttributes.InfoBox("1.Define object\n2. Use button to preview and instanciate\n3. Don't forget to drink water <3")]
    [HorizontalLine(1)]
    [SerializeField]
    private SpawnDefinition[] m_objectToSpawn;
    [SerializeField]
    private Vector3 m_spawnAxisScales = Vector3.one;
    [SerializeField]
    private Vector3 m_spawnOffset = Vector3.zero;
    [SerializeField]
    private float m_spawnRadius = 1f;

    [SerializeField]
    private bool spawnOnStart = false;
    [SerializeField]
    private bool m_useRandomRotation = false;
    public Transform InSceneParent;

    private Dictionary<SpawnDefinition, Vector3[]> m_spawningPoints = new Dictionary<SpawnDefinition, Vector3[]>();

    private void Start()
    {
        if (!spawnOnStart)
        {
            return;
        }

        SpawnAndDestroySpawner();
    }

    [Button]
    private void GeneratePreview()
    {
        Randomize();
    }

    [Button]
    public void SpawnAndDestroySpawner()
    {
        if (m_objectToSpawn == null || m_objectToSpawn.Length == 0)
        {
            return;
        }

        if (m_spawningPoints.Count == 0)
        {
            GeneratePreview();
        }

        foreach (var spawn in m_spawningPoints)
        {
            for (int i = 0, c = spawn.Value.Length; i < c; ++i)
            {
                Instantiate(spawn.Key.Prefab, transform.position + spawn.Value[i], m_useRandomRotation ? Random.rotation : Quaternion.identity, InSceneParent);
            }
        }

        Destroy(gameObject);
    }

    public void Randomize()
    {
        if (m_objectToSpawn == null || m_objectToSpawn.Length == 0)
        {
            return;
        }

        m_spawningPoints.Clear();
        foreach (var spawn in m_objectToSpawn)
        {
            int count = Random.Range(spawn.InRangeSpawnCount.x, spawn.InRangeSpawnCount.y);
            Vector3[] array = new Vector3[count];

            for (int i = 0; i < count; ++i)
            {
                array[i] = GetLocalSpawnPointInSphere(m_spawnRadius);
            }

            m_spawningPoints.Add(spawn, array);
        }
    }

    protected Vector3 GetLocalSpawnPointInSphere(float radius)
    {
        Vector3 v = Random.insideUnitSphere * radius;
        v.x *= m_spawnAxisScales.x;
        v.y *= m_spawnAxisScales.y;
        v.z *= m_spawnAxisScales.z;
        v += m_spawnOffset;
        return v;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_spawningPoints == null || m_spawningPoints.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_spawnRadius);

        foreach (var spawn in m_spawningPoints)
        {
            Gizmos.color = spawn.Key.PreviewColor;

            for (int i = 0, c = spawn.Value.Length; i < c; ++i)
            {
                Gizmos.DrawWireSphere(transform.position + spawn.Value[i], spawn.Key.PreviewSize);
            }
        }
    }
}