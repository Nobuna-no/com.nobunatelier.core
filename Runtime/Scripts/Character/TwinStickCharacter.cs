using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class TwinStickCharacter : Character
    {
        [Header("Twin Stick Movement")]
        [SerializeField]
        private float m_moveSpeed = 10;

        [SerializeField, ReadOnly]
        private float m_orientedAngle = 0;

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

        public virtual float GetMovementAngle()
        {
            return m_orientedAngle;
        }

        public override void Move(Vector3 direction)
        {
            // need to remove delta time dependency
            m_hasReceivedInputThisFrame = true;
            m_lastMoveVector = direction;

            Vector2 dir = new Vector2(transform.forward.x, transform.forward.z);
            Vector2 vel = new Vector2(m_lastMoveVector.x, m_lastMoveVector.z);
            m_orientedAngle = Vector2.SignedAngle(vel, dir) + 180;
            m_orientedAngle /= 360f;
        }

        public override void ProceduralMove(Vector3 movement)
        {
            m_characterController.Move(movement);
        }

        public override void Rotate(Vector3 normalizedDirection)
        {
            transform.rotation = TowDownDirectionToQuaternion(normalizedDirection);
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

        protected virtual void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            if (m_hasReceivedInputThisFrame)
            {
                m_lastMoveSpeed = m_lastMoveVector.magnitude / deltaTime;
                m_characterController.Move(m_lastMoveVector * m_moveSpeed * deltaTime);
                m_hasConsumedInputLastUpdate = true;
                m_hasReceivedInputThisFrame = false;
            }

            m_characterController.Move(Physics.gravity * deltaTime);
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

        private static Quaternion TowDownDirectionToQuaternion(Vector3 normalizedDirection)
        {
            return Quaternion.Euler(new Vector3(0, Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg, 0));
        }
    }
}