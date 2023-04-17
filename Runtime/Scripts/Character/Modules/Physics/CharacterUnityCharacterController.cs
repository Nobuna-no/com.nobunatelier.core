using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/PhysicsModule CharacterController")]
    public class CharacterUnityCharacterController : CharacterPhysicsModule
    {
        [SerializeField]
        private UnityEngine.CharacterController m_targetCharacterController;
        [SerializeField]
        private Vector3 m_maxVelocity = new Vector3(10f, 20f, 10f);

        [SerializeField]
        private bool m_useSimpleMove = false;
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

        public override Vector3 Velocity { get; set; }

        public override Quaternion Rotation
        {
            get
            {
                return ModuleOwner.gameObject.transform.rotation;
            }
            set
            {
                ModuleOwner.gameObject.transform.rotation = value;
            }
        }

        public override bool IsGrounded => m_targetCharacterController.isGrounded;

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
                Debug.LogWarning($"No Unity LegacyCharacterControllerBase found on {ModuleOwner}, instancing default one.");
                m_targetCharacterController = ModuleOwner.gameObject.AddComponent<UnityEngine.CharacterController>();
            }
        }

        public override void ApplyVelocity(Vector3 newVelocity, float deltaTime)
        {
            newVelocity.x = Mathf.Clamp(newVelocity.x, -m_maxVelocity.x, m_maxVelocity.x);
            newVelocity.y = Mathf.Clamp(newVelocity.y, -m_maxVelocity.y, m_maxVelocity.y);
            newVelocity.z = Mathf.Clamp(newVelocity.z, -m_maxVelocity.z, m_maxVelocity.z);

            if (m_useSimpleMove)
            {
                m_targetCharacterController.SimpleMove(newVelocity);
            }
            else
            {
                m_targetCharacterController.Move(newVelocity * deltaTime);
            }

            Velocity = newVelocity;
        }
    }

}
