using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterUnityCharacterController : CharacterBodyModuleBase
    {
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
            get => m_body.transform.position;
            set => m_body.transform.position = value;
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

        public override bool IsGrounded => m_body.isGrounded;

        private UnityEngine.CharacterController m_body;
        public override void ModuleInit(AtelierCharacter character)
        {
            base.ModuleInit(character);
            m_body = ModuleOwner.GetComponent<UnityEngine.CharacterController>();

            if (m_body == null)
            {
                Debug.LogWarning($"No Unity CharacterController found on {ModuleOwner}, instancing default one.");
                m_body = ModuleOwner.gameObject.AddComponent<UnityEngine.CharacterController>();
            }
        }

        public override void ApplyVelocity(Vector3 newVelocity, float deltaTime)
        {
            newVelocity.x = Mathf.Clamp(newVelocity.x, -m_maxVelocity.x, m_maxVelocity.x);
            newVelocity.y = Mathf.Clamp(newVelocity.y, -m_maxVelocity.y, m_maxVelocity.y);
            newVelocity.z = Mathf.Clamp(newVelocity.z, -m_maxVelocity.z, m_maxVelocity.z);

            if (m_useSimpleMove)
            {
                m_body.SimpleMove(newVelocity);
            }
            else
            {
                m_body.Move(newVelocity * deltaTime);
            }

            Velocity = newVelocity;
        }
    }

}
