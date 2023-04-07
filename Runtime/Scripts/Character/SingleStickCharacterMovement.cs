using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class SingleStickCharacterMovement : CharacterMovement
    {
        [Header("Single Stick Movement")]
        [SerializeField]
        private float m_moveSpeed = 10;

        [SerializeField, Range(0, 1)]
        private float m_rotationSpeed = .5f;

        [Header("Animation")]
        [SerializeField, Required, InfoBox("Procedurally filled by Game Mode, but require manual set for testing.")]
        private Animator m_animator;

        // public Animator Animator => m_animator;

        [SerializeField, AnimatorParam("m_animator")]
        private string m_moveSpeedFloatName;

        [SerializeField, ReadOnly]
        protected Vector3 m_lastMoveVector;

        protected UnityEngine.CharacterController m_movement;
        private float m_lastMoveSpeed;

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

        public override void Move(Vector3 normalizedDirection, float deltaTime)
        {
            m_lastMoveVector = normalizedDirection * m_moveSpeed * deltaTime;
            m_lastMoveSpeed = m_lastMoveVector.magnitude / deltaTime;
        }

        public override void ProceduralMove(Vector3 movement)
        {
            m_movement.Move(movement);
        }

        public override void MouseAim(Vector3 normalizedDirection)
        { }

        public override void StickAim(Vector3 normalizedDirection)
        { }

        public override void SetForward(Vector3 dir, float stepSpeed)
        {
            transform.transform.forward = Vector3.Slerp(transform.transform.forward, dir, stepSpeed);
        }

        public override void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            // m_movement.enabled = false;
            base.ResetCharacter(position, rotation);
            Physics.SyncTransforms();
            // m_movement.enabled = true;
            //transform.SetPositionAndRotation(position, rotation);
        }

        public override void ResetCharacter(Transform transform)
        {
            base.ResetCharacter(transform);
            Physics.SyncTransforms();
        }
        private void OnValidate()
        {
            m_movement = GetComponent<UnityEngine.CharacterController>();
        }

        protected override void Awake()
        {
            base.Awake();
            m_movement = GetComponent<UnityEngine.CharacterController>();
        }

        protected virtual void Update()
        {
            m_movement.Move(m_lastMoveVector);
            m_movement.Move(Physics.gravity * Time.deltaTime);
            SetForward(m_lastMoveVector, m_rotationSpeed);

            if (Animator != null)
            {
                Animator.SetFloat(m_moveSpeedFloatName, GetMoveSpeed());
            }

            m_lastMoveVector = Vector2.zero;
            m_lastMoveSpeed = 0;
        }
    }
}