using NobunAtelier;
using UnityEngine;

public class AttackAnimationDefinition : AttackDefinition
{
    public AnimMontageDefinition AnimMontage => m_animation;

    [Header("Animation")]
    [SerializeField]
    private AnimMontageDefinition m_animation;
}