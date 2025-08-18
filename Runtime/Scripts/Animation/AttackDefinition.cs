using NobunAtelier;
using NobunAtelier.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

public class AttackDefinition : DataDefinition
{
    public enum AttackPositioning
    {
        RelativeToSelf,
        RelativeToTarget
    }

    public HitDefinition Hit => m_HitDefinition;
    public TeamDefinition.Target HitTarget => m_HitTarget;

    public AttackPositioning HitPositioning => m_HitPositioning;

    public LoadableHitbox HitboxReference => m_HitboxReference;
    public Vector3 HitboxOffset => m_HitboxOffset;
    public Vector3 HitboxRotation => m_HitboxRotation;
    public Vector3 HitboxScale => m_HitboxScale;

    public LoadableParticleSystem ImpactParticleReference => m_ImpactParticleReference;
    public Vector3 ParticleOffset => m_ParticleOffset;
    public Vector3 ParticleRotation => m_ParticleRotation;
    public Vector3 ParticleScale => m_ParticleScale;

    [Header("Attack")]
    [SerializeField, FormerlySerializedAs("m_hitDefinition")]
    private HitDefinition m_HitDefinition;

    [SerializeField, FormerlySerializedAs("m_hitTarget")]
    private TeamDefinition.Target m_HitTarget = TeamDefinition.Target.Enemies;

    [SerializeField, FormerlySerializedAs("m_hitboxReference")]
    private LoadableHitbox m_HitboxReference;

    [SerializeField, FormerlySerializedAs("m_hitPositioning")]
    private AttackPositioning m_HitPositioning = AttackPositioning.RelativeToSelf;

    [SerializeField, FormerlySerializedAs("m_hitboxOffset")]
    private Vector3 m_HitboxOffset = Vector3.zero;

    [SerializeField, FormerlySerializedAs("m_hitboxRotation")]
    private Vector3 m_HitboxRotation = Vector3.zero;

    [SerializeField, FormerlySerializedAs("m_hitboxScale")]
    private Vector3 m_HitboxScale = Vector3.one;

    [SerializeField, FormerlySerializedAs("m_impactParticleReference")]
    private LoadableParticleSystem m_ImpactParticleReference;

    [SerializeField, FormerlySerializedAs("m_particleOffset")]
    private Vector3 m_ParticleOffset = Vector3.zero;

    [SerializeField, FormerlySerializedAs("m_particleRotation")]
    private Vector3 m_ParticleRotation = Vector3.zero;

    [SerializeField, FormerlySerializedAs("m_particleScale")]
    private Vector3 m_ParticleScale = Vector3.one;
}