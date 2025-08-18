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

        [SerializeField]
        private float m_maxSpeed = 20f;

        [SerializeField]
        private LayerMask m_groundLayers;

        [SerializeField]
        private bool m_useGravity = false;

        [SerializeField]
        private bool m_freezeRotation = true;

        [SerializeField]
        private bool m_isKinematic = false;

        [SerializeField]
        private float m_groundCheckDistance = 0.1f;

        [SerializeField]
        private float m_groundedGracePeriod = 0.1f;

        [SerializeField]
        private LayerMask m_collisionMask = ~0; // All layers by default

        [SerializeField]
        private float m_skinWidth = 0.01f;

        private bool m_hasChangeGroundedThisFrame = false;
        private Vector3 m_kinematicVelocity = Vector3.zero;
        private Vector3 m_currentVelocity = Vector3.zero;
        private bool m_isGrounded = false;
        private float m_lastGroundedTime = 0f;
        private RaycastHit m_groundHit;
        private const int kGroundCheckRays = 4;
        private Collider m_collider;

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

            m_collider = m_targetRigidbody.GetComponent<Collider>();
            if (m_collider == null)
            {
                Debug.LogWarning($"No collider found on {ModuleOwner}, kinematic collision will not work.");
            }

            m_targetRigidbody.freezeRotation = m_freezeRotation;
            m_targetRigidbody.useGravity = m_useGravity && !m_isKinematic;
            m_targetRigidbody.isKinematic = m_isKinematic;
            m_currentVelocity = Vector3.zero;
            m_kinematicVelocity = Vector3.zero;
        }

        public override void ApplyVelocity(Vector3 newVelocity, float deltaTime)
        {
            // Apply velocity limits
            newVelocity.x = Mathf.Clamp(newVelocity.x, -m_maxVelocity.x, m_maxVelocity.x);
            newVelocity.y = Mathf.Clamp(newVelocity.y, -m_maxVelocity.y, m_maxVelocity.y);
            newVelocity.z = Mathf.Clamp(newVelocity.z, -m_maxVelocity.z, m_maxVelocity.z);
            newVelocity = Vector3.ClampMagnitude(newVelocity, m_maxSpeed);

            if (m_targetRigidbody.isKinematic)
            {
                HandleKinematicMovement(newVelocity, deltaTime);
            }
            else
            {
                HandleDynamicMovement(newVelocity, deltaTime);
            }

            // Update grounded state
            CheckGroundedState();
        }

        private void HandleKinematicMovement(Vector3 newVelocity, float deltaTime)
        {
            m_kinematicVelocity = newVelocity;

            if (m_useGravity && !m_isGrounded)
            {
                m_kinematicVelocity += Physics.gravity * deltaTime;
            }

            Vector3 movement = m_kinematicVelocity * deltaTime;
            Vector3 targetPosition = m_targetRigidbody.position;
            
            if (m_collider != null)
            {
                // Use sweep test for more accurate collision detection
                SphereCollider sphereCollider = m_collider as SphereCollider;
                CapsuleCollider capsuleCollider = m_collider as CapsuleCollider;
                BoxCollider boxCollider = m_collider as BoxCollider;

                if (sphereCollider != null)
                {
                    if (Physics.SphereCast(targetPosition, sphereCollider.radius, movement.normalized, out RaycastHit hit, movement.magnitude + m_skinWidth, m_collisionMask))
                    {
                        float distance = hit.distance - m_skinWidth;
                        movement = movement.normalized * distance;
                    }
                }
                else if (capsuleCollider != null)
                {
                    // Get capsule points
                    float height = capsuleCollider.height;
                    float radius = capsuleCollider.radius;
                    Vector3 center = capsuleCollider.center;
                    
                    Vector3 point1 = targetPosition + center;
                    Vector3 point2 = point1;
                    
                    switch (capsuleCollider.direction) // 0 = X, 1 = Y, 2 = Z
                    {
                        case 0: // X axis
                            point1.x -= height * 0.5f - radius;
                            point2.x += height * 0.5f - radius;
                            break;
                        case 1: // Y axis
                            point1.y -= height * 0.5f - radius;
                            point2.y += height * 0.5f - radius;
                            break;
                        case 2: // Z axis
                            point1.z -= height * 0.5f - radius;
                            point2.z += height * 0.5f - radius;
                            break;
                    }

                    if (Physics.CapsuleCast(point1, point2, radius, movement.normalized, out RaycastHit hit, movement.magnitude + m_skinWidth, m_collisionMask))
                    {
                        float distance = hit.distance - m_skinWidth;
                        movement = movement.normalized * distance;
                    }
                }
                else if (boxCollider != null)
                {
                    // For box collider, we'll use a simple raycast from each corner
                    Vector3 size = boxCollider.size * 0.5f;
                    Vector3 center = boxCollider.center;
                    Vector3[] corners = new Vector3[8];
                    
                    corners[0] = targetPosition + center + new Vector3(-size.x, -size.y, -size.z);
                    corners[1] = targetPosition + center + new Vector3(-size.x, -size.y, size.z);
                    corners[2] = targetPosition + center + new Vector3(-size.x, size.y, -size.z);
                    corners[3] = targetPosition + center + new Vector3(-size.x, size.y, size.z);
                    corners[4] = targetPosition + center + new Vector3(size.x, -size.y, -size.z);
                    corners[5] = targetPosition + center + new Vector3(size.x, -size.y, size.z);
                    corners[6] = targetPosition + center + new Vector3(size.x, size.y, -size.z);
                    corners[7] = targetPosition + center + new Vector3(size.x, size.y, size.z);

                    float shortestDistance = movement.magnitude + m_skinWidth;
                    
                    foreach (Vector3 corner in corners)
                    {
                        if (Physics.Raycast(corner, movement.normalized, out RaycastHit hit, movement.magnitude + m_skinWidth, m_collisionMask))
                        {
                            if (hit.distance < shortestDistance)
                            {
                                shortestDistance = hit.distance;
                            }
                        }
                    }

                    if (shortestDistance < movement.magnitude + m_skinWidth)
                    {
                        movement = movement.normalized * (shortestDistance - m_skinWidth);
                    }
                }
            }

            m_targetRigidbody.MovePosition(targetPosition + movement);
        }

        private void HandleDynamicMovement(Vector3 newVelocity, float deltaTime)
        {
            if (m_useGravity)
            {
                // Only preserve gravity if we're applying a non-jump velocity
                // Jump velocity from modules should override gravity
                if (!m_isGrounded && Mathf.Approximately(newVelocity.y, m_targetRigidbody.linearVelocity.y))
                {
                    newVelocity.y = m_targetRigidbody.linearVelocity.y;
                }
            }

            // Apply the new velocity
            m_targetRigidbody.linearVelocity = newVelocity;
            m_currentVelocity = newVelocity;
        }

        protected override bool CheckGroundedState()
        {
            bool wasGrounded = m_isGrounded;
            bool newGroundedState = false;

            // Center position for ground checks
            Vector3 position = m_targetRigidbody.position;
            float radius = m_targetRigidbody.GetComponent<Collider>()?.bounds.extents.x ?? 0.5f;

            // Multiple raycasts from different points
            for (int i = 0; i < kGroundCheckRays; i++)
            {
                float angle = i * (360f / kGroundCheckRays);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius * 0.75f);
                Vector3 rayStart = position + offset + Vector3.up * 0.1f;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, m_groundCheckDistance + 0.1f, m_groundLayers))
                {
                    m_groundHit = hit;
                    newGroundedState = true;
                    break;
                }
            }

            // If not grounded by raycasts, try spherecast as backup
            if (!newGroundedState)
            {
                if (Physics.SphereCast(position + Vector3.up * 0.1f, radius * 0.5f, Vector3.down, out m_groundHit, m_groundCheckDistance, m_groundLayers))
                {
                    newGroundedState = true;
                }
            }

            // Update grounded state with grace period
            if (newGroundedState)
            {
                m_isGrounded = true;
                m_lastGroundedTime = Time.time;
            }
            else if (Time.time - m_lastGroundedTime > m_groundedGracePeriod)
            {
                m_isGrounded = false;
            }

            // Track grounded state changes
            if (wasGrounded != m_isGrounded)
            {
                m_hasChangeGroundedThisFrame = true;
            }

            return m_isGrounded;
        }

        public override void OnModuleCollisionEnter(Collision collision)
        {
            if ((m_groundLayers.value & (1 << collision.gameObject.layer)) == 0)
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

            if ((m_groundLayers.value & (1 << collision.gameObject.layer)) == 0)
            {
                return;
            }

            m_isGrounded = true;
        }

        public override void OnModuleCollisionExit(Collision collision)
        {
            if ((m_groundLayers.value & (1 << collision.gameObject.layer)) == 0)
            {
                return;
            }

            m_isGrounded = false;
        }
    }
}