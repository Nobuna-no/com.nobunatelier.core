using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [System.Serializable]
    private class SpawnDefinition
    {
        public GameObject Prefab;

        [Required]
        public Transform InSceneParent;

        [NaughtyAttributes.MinMaxSlider(1, 20)]
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
    private float m_spawnRadius = 1f;

    private Dictionary<SpawnDefinition, Vector3[]> m_previews = new Dictionary<SpawnDefinition, Vector3[]>();

    private void Start()
    {
        Destroy(this);
    }

    [Button]
    private void GeneratePreview()
    {
        if (m_objectToSpawn == null || m_objectToSpawn.Length == 0)
        {
            return;
        }

        m_previews.Clear();
        foreach (var spawn in m_objectToSpawn)
        {
            int count = Random.Range(spawn.InRangeSpawnCount.x, spawn.InRangeSpawnCount.y);
            Vector3[] array = new Vector3[count];

            for (int i = 0; i < count; ++i)
            {
                array[i] = GetLocalSpawnPointInSphere(m_spawnRadius);
            }

            m_previews.Add(spawn, array);
        }
    }

    [Button]
    private void Spawn()
    {
        if (m_objectToSpawn == null || m_objectToSpawn.Length == 0)
        {
            return;
        }

        if (m_previews.Count == 0)
        {
            GeneratePreview();
        }

        foreach (var spawn in m_previews)
        {
            for (int i = 0, c = spawn.Value.Length; i < c; ++i)
            {
                Instantiate(spawn.Key.Prefab, transform.position + spawn.Value[i], Random.rotation, spawn.Key.InSceneParent);
            }
        }
    }

    protected Vector3 GetLocalSpawnPointInSphere(float radius)
    {
        return Random.insideUnitSphere * radius;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_previews == null || m_previews.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_spawnRadius);

        foreach (var spawn in m_previews)
        {
            Gizmos.color = spawn.Key.PreviewColor;

            for (int i = 0, c = spawn.Value.Length; i < c; ++i)
            {
                Gizmos.DrawWireSphere(transform.position + spawn.Value[i], spawn.Key.PreviewSize);
            }
        }
    }
}