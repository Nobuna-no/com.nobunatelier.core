using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class MovementAnimationModule : AnimationModule
    {
        [SerializeField, AnimatorParam("m_animator")]
        private string m_moveSpeedFloatName;
        [SerializeField, Min(0f)] private float m_motionDamping = 1f;
        private float m_smoothSpeed = 0;
        protected override void OnAbilityUpdate(float deltaTime)
        {
            var movement = ModuleOwner.GetMoveVector();
            movement.y = 0;

            m_smoothSpeed = Mathf.Lerp(m_smoothSpeed, movement.sqrMagnitude, deltaTime * m_motionDamping);

            Animator.SetFloat(m_moveSpeedFloatName, m_smoothSpeed);
        }
    }
}