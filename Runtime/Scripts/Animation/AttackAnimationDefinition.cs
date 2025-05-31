using NobunAtelier;
using UnityEngine;
using UnityEngine.Serialization;

public class AttackAnimationDefinition : AttackDefinition
{
    public AnimMontageDefinition AnimMontage => m_Animation;

    [Header("Animation")]
    [SerializeField, FormerlySerializedAs("m_animation")]
    private AnimMontageDefinition m_Animation;
}