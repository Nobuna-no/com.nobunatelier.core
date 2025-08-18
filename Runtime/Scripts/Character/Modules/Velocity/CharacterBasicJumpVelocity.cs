using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: Basic Jump")]
    public class CharacterBasicJumpVelocity : CharacterVelocityModuleBase
    {
        [SerializeField, FormerlySerializedAs("m_jumpHeight")]
        private float m_JumpHeight = 20;

        [SerializeField, FormerlySerializedAs("m_maxJumpCount")]
        private int m_MaxJumpCount = 1;

        [SerializeField, FormerlySerializedAs("m_currentJumpCount")]
        private int m_CurrentJumpCount = 0;

        [SerializeField]
        private UnityEvent m_OnJump;

        [SerializeField, Tooltip("The time in seconds that a jump input will be buffered, allowing players to press jump slightly before landing")]
        private float m_InputBufferTime = 0.2f;

        private bool m_CanJump = true;
        private CharacterInputBuffer m_JumpBuffer;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            m_JumpBuffer = new CharacterInputBuffer(m_InputBufferTime);
        }

        public void DoJump()
        {
            if (m_CanJump)
            {
                m_JumpBuffer.RequestAction();
            }
        }

        private float Jump()
        {
            ++m_CurrentJumpCount;
            m_JumpBuffer.ConsumeRequest();
            m_OnJump?.Invoke();
            return Mathf.Sqrt(2f * -Physics.gravity.y * m_JumpHeight);
        }

        public override void StateUpdate(bool grounded)
        {
            if (grounded)
            {
                m_CurrentJumpCount = 0;
            }

            m_CanJump = m_CurrentJumpCount < m_MaxJumpCount;
        }

        public override bool CanBeExecuted()
        {
            return base.CanBeExecuted() && m_JumpBuffer.HasActiveRequest() && m_CanJump;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            currentVel.y = Jump();
            return currentVel;
        }
    }
}