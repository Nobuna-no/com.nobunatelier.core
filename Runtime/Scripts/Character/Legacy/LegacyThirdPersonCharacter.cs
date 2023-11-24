using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class LegacyThirdPersonCharacter : LegacyCharacterBase
    {
        [SerializeField]
        private float m_moveSpeed = 10;

        [SerializeField, ReadOnly]
        protected Vector3 m_lastMoveVector;

        protected UnityEngine.CharacterController m_characterController;
        private float m_lastMoveSpeed;

        private bool m_hasReceivedInputThisFrame = false;
        private bool m_hasConsumedInputLastUpdate = false;

        public override Vector3 GetMoveVector()
        {
            return m_lastMoveVector;
        }

        public override float GetMoveSpeed()
        {
            return m_lastMoveSpeed;
        }

        public override float GetNormalizedMoveSpeed()
        {
            return m_lastMoveSpeed / m_moveSpeed;
        }

        public override void Move(Vector3 normalizedDirection)
        {
            m_hasReceivedInputThisFrame = true;
            m_lastMoveVector = normalizedDirection * m_moveSpeed;
        }

        private void OnValidate()
        {
            m_characterController = GetComponent<UnityEngine.CharacterController>();
        }

        protected override void Awake()
        {
            base.Awake();
            m_characterController = GetComponent<UnityEngine.CharacterController>();
        }

        protected virtual void Update()
        {
            if (m_hasReceivedInputThisFrame)
            {
                m_characterController.Move(m_lastMoveVector * Time.deltaTime);
                m_lastMoveSpeed = m_lastMoveVector.magnitude / Time.deltaTime;
                m_hasConsumedInputLastUpdate = true;
                m_hasReceivedInputThisFrame = false;
            }

            m_characterController.Move(Physics.gravity * Time.deltaTime);
        }

        protected virtual void LateUpdate()
        {
            if (m_hasConsumedInputLastUpdate)
            {
                m_lastMoveVector = Vector2.zero;
                m_lastMoveSpeed = 0;
                m_hasConsumedInputLastUpdate = false;
                m_hasReceivedInputThisFrame = false;
            }
        }
    }
}