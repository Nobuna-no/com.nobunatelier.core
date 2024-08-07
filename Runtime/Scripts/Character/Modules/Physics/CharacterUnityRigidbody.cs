using NaughtyAttributes;
using UnityEngine;
using static NobunAtelier.ContextualLogManager;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Physics/PhysicsModule: Rigidbody")]
    public class CharacterUnityRigidBody : CharacterPhysicsModule
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

        private bool m_hasChangeGroundedThisFrame = false;

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
                return m_targetRigidbody.isKinematic ? m_kinematicVelocity : m_targetRigidbody.linearVelocity;
            }
            set
            {
                if (m_targetRigidbody.isKinematic)
                {
                    m_kinematicVelocity = value;
                }
                else
                {
                    m_targetRigidbody.linearVelocity = value;
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

        public override void ModuleInit(Character character)
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
                // If it there is an incoherence between the rigidbody and this module,
                // assume that the player wanted to prevent physics movement.
                if (m_isKinematic == false)
                {
                    m_kinematicVelocity = Vector3.zero;
                }
                else
                {
                    m_targetRigidbody.position += newVelocity * deltaTime;
                    m_kinematicVelocity = newVelocity;
                }
            }
            else
            {
                if (m_useGravity)
                {
                    if (Physics.gravity.x != 0)
                    {
                        newVelocity.x += m_targetRigidbody.linearVelocity.x;
                    }
                    if (Physics.gravity.y != 0)
                    {
                        newVelocity.y += m_targetRigidbody.linearVelocity.y;
                    }
                    if (Physics.gravity.z != 0)
                    {
                        newVelocity.z += m_targetRigidbody.linearVelocity.z;
                    }
                }

                m_targetRigidbody.linearVelocity = newVelocity;

                if (newVelocity.y != 0 && m_isGrounded)
                {
                    m_isGrounded = false;
                    m_hasChangeGroundedThisFrame = true;
                }
            }
        }

        public override void OnModuleCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer != m_groundLayer)
            {
                return;
            }

            m_isGrounded = true;
        }

        public override void OnModuleCollisionStay(Collision collision)
        {
            if (m_hasChangeGroundedThisFrame)
            {
                m_hasChangeGroundedThisFrame = false;
                return;
            }

            if (collision.gameObject.layer != m_groundLayer)
            {
                return;
            }

            m_isGrounded = true;
        }

        public override void OnModuleCollisionExit(Collision collision)
        {
            if (collision.gameObject.layer != m_groundLayer)
            {
                return;
            }

            m_isGrounded = false;
        }
    }
}