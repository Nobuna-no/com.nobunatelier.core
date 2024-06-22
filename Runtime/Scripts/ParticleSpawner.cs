using NaughtyAttributes;
using NobunAtelier;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField, Required]
    private FactoryProductDefinition m_poolObjectID;

    [SerializeField, Required]
    private Transform m_parent;

    [SerializeField]
    private Vector3 m_spawnRadiusAxis = Vector3.one;

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void SpawnParticleOnTarget()
    {
        if (m_poolObjectID && m_parent)
        {
            var product = DataDrivenFactoryManager.Get(m_poolObjectID);
            product.Position = GetSpawnPointInRadius(m_parent.position);
        }
    }

    protected Vector3 GetSpawnPointInRadius(Vector3 location)
    {
        Vector3 circlePos = Random.insideUnitSphere;
        return new Vector3(m_spawnRadiusAxis.x * circlePos.x, m_spawnRadiusAxis.y * circlePos.y,
            m_spawnRadiusAxis.z * circlePos.z) + location;
    }
}