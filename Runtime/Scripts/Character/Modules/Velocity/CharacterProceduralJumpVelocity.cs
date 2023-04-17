using System;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterProceduralJumpVelocity : CharacterVelocityModule
    {
        [SerializeField]
        private float m_jumpHeight = 20;
        [SerializeField, Range(0f, 1f)]
        private float m_durationInSeconds = 0.5f;
        [SerializeField]
        private AnimationCurve m_accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        // [SerializeField]
        // private int m_maxJumpCount = 1;
        //
        // [SerializeField, Range(0f, 1f)]
        // private float m_multiJumpDelay = 0.5f;

        private float m_currentJumpTime = -1f;

        private int m_currentJumpCount = 0;
        private bool m_canJump = true;
        private bool m_isJumping = false;
        private bool m_wantToJump = false;
        private bool m_hasJumpedThisFrame = false;

        public void DoJump()
        {
            if (m_canJump)
            {
                m_wantToJump = true;
                m_currentJumpTime = 0;
            }
        }

        private float Jump()
        {
            ++m_currentJumpCount;
            m_wantToJump = false;
            m_hasJumpedThisFrame = true;
            return Mathf.Sqrt(2f * -Physics.gravity.y * m_jumpHeight);
        }

        public override void StateUpdate(bool grounded)
        {
            if (grounded)
            {
                m_currentJumpCount = 0;
                m_canJump = true;
            }

            //m_canJump = m_currentJumpCount < m_maxJumpCount;
        }

        public override bool CanBeExecuted()
        {
            return m_isJumping || (base.CanBeExecuted() && m_wantToJump && m_canJump);
        }

        public override bool StopVelocityUpdate()
        {
            // bool stopUpdate = m_hasJumpedThisFrame;
            // m_hasJumpedThisFrame = false;
            // return stopUpdate;
            return false;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            if (m_currentJumpTime == -1)
            {
                return currentVel;
            }

            m_isJumping = true;

            m_hasJumpedThisFrame = true;
            m_currentJumpTime += deltaTime / m_durationInSeconds;
            currentVel.y = m_jumpHeight * m_accelerationCurve.Evaluate(m_currentJumpTime);

            if (m_currentJumpTime >= 1)
            {
                m_currentJumpTime = -1;
                m_wantToJump = false;
                m_isJumping = false;
            }

            return currentVel;
        }
    }
}