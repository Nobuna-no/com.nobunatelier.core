using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class LegacySingleStickCharacter : LegacyCharacterBase
    {
        [Header("Single Stick Movement")]
        [SerializeField, FormerlySerializedAs("m_moveSpeed")]
        private float m_MoveSpeed = 10;

        [SerializeField, Range(0, 1), FormerlySerializedAs("m_rotationSpeed")]
        private float m_RotationSpeed = .5f;

        [Header("Animation"), FormerlySerializedAs("m_animator")]
        [SerializeField, Required, InfoBox("Procedurally filled by Game Mode, but require manual set for testing.")]
        private Animator m_Animator;

        // public Animator Animator => m_animator;

        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_moveSpeedFloatName")]
        private string m_MoveSpeedFloatName;

        [SerializeField, ReadOnly]
        protected Vector3 m_LastMoveVector;

        protected UnityEngine.CharacterController m_Movement;
        private float m_LastMoveSpeed;

        public void SetSpeed(float speed)
        {
            m_MoveSpeed = speed;
        }

        public void SetRotationSpeed(float speed)
        {
            m_RotationSpeed = speed;
        }

        public override Vector3 GetMoveVector()
        {
            return m_LastMoveVector;
        }

        public override float GetMoveSpeed()
        {
            return m_LastMoveSpeed;
        }

        public override float GetNormalizedMoveSpeed()
        {
            return m_LastMoveSpeed / m_MoveSpeed;
        }

        public override void Move(Vector3 direction)
        {
            m_LastMoveVector = direction.normalized; ;
        }

        public override void ProceduralMove(Vector3 movement)
        {
            m_Movement.Move(movement);
        }

        public override void Rotate(Vector3 normalizedDirection)
        { }

        private void SetForward(Vector3 dir, float stepSpeed)
        {
            transform.transform.forward = Vector3.Slerp(transform.transform.forward, dir, stepSpeed);
        }

        private void OnValidate()
        {
            m_Movement = GetComponent<UnityEngine.CharacterController>();
        }

        protected override void Awake()
        {
            base.Awake();
            m_Movement = GetComponent<UnityEngine.CharacterController>();
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;
            m_LastMoveSpeed = (m_LastMoveVector.magnitude * m_MoveSpeed) / deltaTime;
            m_Movement.Move(m_LastMoveVector * m_MoveSpeed * deltaTime);
            m_Movement.Move(Physics.gravity * deltaTime);
            SetForward(m_LastMoveVector, m_RotationSpeed);

            if (Animator != null)
            {
                Animator.SetFloat(m_MoveSpeedFloatName, m_LastMoveSpeed);
            }

            m_LastMoveVector = Vector2.zero;
            m_LastMoveSpeed = 0;
        }
    }
}