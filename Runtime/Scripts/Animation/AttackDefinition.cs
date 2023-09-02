using NobunAtelier;
using NobunAtelier.Gameplay;
using UnityEngine;

public class AttackDefinition : DataDefinition
{
    public HitDefinition Hit => m_hitDefinition;
    public TeamDefinition.Target HitTarget => m_hitTarget;

    public AssetReferenceHitboxBehaviour HitboxReference => m_hitboxReference;
    public Vector3 HitboxOffset => m_hitboxOffset;
    public Vector3 HitboxRotation => m_hitboxRotation;
    public Vector3 HitboxScale => m_hitboxScale;

    public AssetReferenceParticleSystem ImpactParticleReference => m_impactParticleReference;
    public Vector3 ParticleOffset => m_particleOffset;
    public Vector3 ParticleRotation => m_particleRotation;
    public Vector3 ParticleScale => m_particleScale;

    [Header("Attack")]
    [SerializeField]
    private HitDefinition m_hitDefinition;
    [SerializeField]
    private TeamDefinition.Target m_hitTarget = TeamDefinition.Target.Enemies;

    [SerializeField]
    private AssetReferenceHitboxBehaviour m_hitboxReference;

    [SerializeField]
    private Vector3 m_hitboxOffset = Vector3.zero;

    [SerializeField]
    private Vector3 m_hitboxRotation = Vector3.zero;

    [SerializeField]
    private Vector3 m_hitboxScale = Vector3.one;

    [SerializeField]
    private AssetReferenceParticleSystem m_impactParticleReference;

    [SerializeField]
    private Vector3 m_particleOffset = Vector3.zero;

    [SerializeField]
    private Vector3 m_particleRotation = Vector3.zero;

    [SerializeField]
    private Vector3 m_particleScale = Vector3.one;
}