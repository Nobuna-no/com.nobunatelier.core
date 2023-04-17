using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/VelocityModule Basic Jump")]
    public class CharacterBasicJumpVelocity : CharacterVelocityModule
    {
        [SerializeField]
        private float m_jumpHeight = 20;
        [SerializeField]
        private int m_maxJumpCount = 1;

        private int m_currentJumpCount = 0;
        private bool m_canJump = true;
        private bool m_wantToJump = false;

        public void DoJump()
        {
            if (m_canJump)
            {
                m_wantToJump = true;
            }
        }

        private float Jump()
        {
            // ModuleOwner.Body.IsGrounded = false;
            ++m_currentJumpCount;
            m_wantToJump = false;
            return Mathf.Sqrt(2f * -Physics.gravity.y * m_jumpHeight);
        }

        public override void StateUpdate(bool grounded)
        {
            if (grounded)
            {
                m_currentJumpCount = 0;
            }

            m_canJump = m_currentJumpCount < m_maxJumpCount;
        }

        public override bool CanBeExecuted()
        {
            return base.CanBeExecuted() && m_wantToJump && m_canJump;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            currentVel.y = Jump();
            return currentVel;
        }
    }
}