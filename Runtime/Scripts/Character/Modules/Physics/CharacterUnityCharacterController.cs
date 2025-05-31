using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Physics/PhysicsModule: CharacterController")]
    public class CharacterUnityCharacterController : CharacterPhysicsModule
    {
        [SerializeField]
        private UnityEngine.CharacterController m_targetCharacterController;

        [SerializeField]
        private Vector3 m_maxVelocity = new Vector3(10f, 20f, 10f);

        [SerializeField]
        private float m_maxSpeed = 20f;

        [SerializeField]
        private bool m_useSimpleMove = false;

        [SerializeField]
        private float m_groundCheckDistance = 0.1f;

        [SerializeField]
        private LayerMask m_groundLayers = -1; // All layers by default

        private Vector3 m_currentVelocity = Vector3.zero;

        public override VelocityApplicationUpdate VelocityUpdate
        {
            get
            {
                return m_useSimpleMove ? VelocityApplicationUpdate.FixedUpdate : VelocityApplicationUpdate.Update;
            }
        }

        public override Vector3 Position
        {
            get => m_targetCharacterController.transform.position;
            set => m_targetCharacterController.transform.position = value;
        }

        public override Vector3 Velocity
        {
            get => m_currentVelocity;
            set => m_currentVelocity = value;
        }

        public override Quaternion Rotation
        {
            get
            {
                return m_targetCharacterController.transform.rotation;
            }
            set
            {
                m_targetCharacterController.transform.rotation = value;
            }
        }

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            if (m_targetCharacterController)
            {
                return;
            }

            // If no character movement assign, try to find one and instantiate in last resort.
            m_targetCharacterController = ModuleOwner.GetComponent<UnityEngine.CharacterController>();
            if (m_targetCharacterController == null)
            {
                Debug.LogWarning($"No Unity CharacterController found on {ModuleOwner}, instancing default one.");
                m_targetCharacterController = ModuleOwner.gameObject.AddComponent<UnityEngine.CharacterController>();
            }
        }

        private bool DoRaycastGroundCheck()
        {
            if (m_targetCharacterController == null)
                return false;
                
            // Get the bottom center of the character controller
            Vector3 rayStart = transform.position + m_targetCharacterController.center;
            rayStart.y -= (m_targetCharacterController.height / 2f - m_targetCharacterController.radius);
            
            // Cast a short ray downward
            return Physics.SphereCast(
                rayStart, 
                m_targetCharacterController.radius * 0.9f, 
                Vector3.down, 
                out RaycastHit hit, 
                m_groundCheckDistance, 
                m_groundLayers
            );
        }

        public override void ApplyVelocity(Vector3 newVelocity, float deltaTime)
        {
            newVelocity.x = Mathf.Clamp(newVelocity.x, -m_maxVelocity.x, m_maxVelocity.x);
            newVelocity.y = Mathf.Clamp(newVelocity.y, -m_maxVelocity.y, m_maxVelocity.y);
            newVelocity.z = Mathf.Clamp(newVelocity.z, -m_maxVelocity.z, m_maxVelocity.z);
            newVelocity = Vector3.ClampMagnitude(newVelocity, m_maxSpeed);

            if (m_useSimpleMove)
            {
                m_targetCharacterController.SimpleMove(newVelocity);
            }
            else
            {
                m_targetCharacterController.Move(newVelocity * deltaTime);
            }

            m_currentVelocity = newVelocity;
        }
        
        protected override bool CheckGroundedState()
        {
            // First check Unity's built-in ground detection
            bool unityGroundCheck = m_targetCharacterController.isGrounded;
            
            // Then do our own raycast check for more reliability
            bool raycastGroundCheck = DoRaycastGroundCheck();
            
            // We're grounded if either method detects ground
            return unityGroundCheck || raycastGroundCheck;
        }

    }
}