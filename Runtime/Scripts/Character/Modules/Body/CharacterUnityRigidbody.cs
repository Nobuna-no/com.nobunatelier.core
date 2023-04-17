using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character Module/Body Rigidbody")]
    public class CharacterUnityRigidBody : CharacterBodyModuleBase
    {
        [SerializeField]
        private Rigidbody m_targetRigidbody;

        [SerializeField]
        private Vector3 m_maxVelocity = new Vector3(10f, 20f, 10f);

        [SerializeField, LayerAttribute]
        private int m_groundLayer;

        [SerializeField]
        private bool m_useGravity = false;

        [SerializeField]
        private bool m_freezeRotation = true;

        [SerializeField, InfoBox("Kinematic implementation in progress...")]
        private bool m_isKinematic = false;

        public override VelocityApplicationUpdate VelocityUpdate => VelocityApplicationUpdate.FixedUpdate;

        public override Vector3 Position
        {
            get => m_targetRigidbody.position;
            set
            {
                m_targetRigidbody.position = value;
            }
        }

        public override Vector3 Velocity
        {
            get
            {
                return m_targetRigidbody.isKinematic ? m_kinematicVelocity : m_targetRigidbody.velocity;
            }
            set
            {
                if (m_targetRigidbody.isKinematic)
                {
                    m_kinematicVelocity = value;
                }
                else
                {
                    m_targetRigidbody.velocity = value;
                }
            }
        }

        public override Quaternion Rotation
        {
            get
            {
                return m_targetRigidbody.rotation;
            }
            set
            {
                m_targetRigidbody.rotation = value;
            }
        }

        public override bool IsGrounded => m_isGrounded;

        private bool m_isGrounded = false;
        private Vector3 m_kinematicVelocity = Vector3.zero;

        public override void ModuleInit(AtelierCharacter character)
        {
            base.ModuleInit(character);

            if (!m_targetRigidbody)
            {
                m_targetRigidbody = ModuleOwner.GetComponent<Rigidbody>();
                if (m_targetRigidbody == null)
                {
                    Debug.LogWarning($"No rigidbody found on {ModuleOwner}, instancing default one.");
                    m_targetRigidbody = ModuleOwner.gameObject.AddComponent<Rigidbody>();
                }
            }

            m_targetRigidbody.freezeRotation = m_freezeRotation;
            m_targetRigidbody.useGravity = m_useGravity;
            m_targetRigidbody.isKinematic = m_isKinematic;

            return;
        }

        public override void ApplyVelocity(Vector3 newVelocity, float deltaTime)
        {
            newVelocity.x = Mathf.Clamp(newVelocity.x, -m_maxVelocity.x, m_maxVelocity.x);
            newVelocity.y = Mathf.Clamp(newVelocity.y, -m_maxVelocity.y, m_maxVelocity.y);
            newVelocity.z = Mathf.Clamp(newVelocity.z, -m_maxVelocity.z, m_maxVelocity.z);

            if (m_targetRigidbody.isKinematic)
            {
                m_targetRigidbody.position += newVelocity * deltaTime;
                m_kinematicVelocity = newVelocity;
            }
            else
            {
                m_targetRigidbody.velocity = newVelocity;
            }

            m_isGrounded = false;
        }

        public override void OnModuleCollisionEnter(Collision collision)
        {
            m_isGrounded = collision.collider.gameObject.layer == m_groundLayer;
        }

        public override void OnModuleCollisionStay(Collision collision)
        {
            m_isGrounded = collision.collider.gameObject.layer == m_groundLayer;
        }

        public override void OnModuleCollisionExit(Collision collision)
        {
            m_isGrounded = false;
        }
    }
}