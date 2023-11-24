using NaughtyAttributes;
using NobunAtelier;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField, Required]
    private PoolObjectDefinition m_poolObjectID;

    [SerializeField, Required]
    private Transform m_targetLocation;

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void SpawnParticleOnTarget()
    {
        if (m_poolObjectID && m_targetLocation)
        {
            ParticlePoolManager.Instance.SpawnObject(m_poolObjectID, m_targetLocation.position);
        }
    }
}