using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class MovementAnimationModule : AnimationModule
    {
        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_moveSpeedFloatName")]
        private string m_MoveSpeedFloatName;
        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_moveSpeedParam")]
        private string m_GroundedBoolName;
        [SerializeField, Min(0f), FormerlySerializedAs("m_motionDamping")]
        private float m_MotionDamping = 1f;
        private float m_SpeedToApply = 0;
        private bool m_Grounded = false;
        protected override void OnAbilityUpdate(float deltaTime)
        {
            var movement = ModuleOwner.GetMoveVector();
            movement.y = 0;

            m_SpeedToApply = movement.magnitude;

            if (m_MotionDamping > 0f)
            {
                m_SpeedToApply = Mathf.Lerp(m_SpeedToApply, m_SpeedToApply, deltaTime * m_MotionDamping);
            }
            m_Grounded = ModuleOwner.Body.IsGrounded;
        }

        private void LateUpdate()
        {
            Animator.SetFloat(m_MoveSpeedFloatName, m_SpeedToApply);
            Animator.SetBool(m_GroundedBoolName, m_Grounded);
        }
    }
}