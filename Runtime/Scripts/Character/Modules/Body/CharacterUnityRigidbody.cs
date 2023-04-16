using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterUnityRigidBody : CharacterBodyModuleBase
    {
        public override Vector3 Position
        {
            get => m_body.position;
            set
            {
                m_body.position = value;
            }
        }

        public override Vector3 Velocity
        {
            get => m_body.velocity;
            set
            {
                m_body.velocity = value;
            }
        }


        public override Quaternion Rotation
        {
            get
            {
                return m_body.rotation;
            }
            set
            {
                m_body.rotation = value;
            }
        }

        public override bool IsGrounded => m_isGrounded;

        private Rigidbody m_body;
        private bool m_isGrounded = false;

        public override void ModuleInit(AtelierCharacter character)
        {
            base.ModuleInit(character);
            m_body = ModuleOwner.GetComponent<Rigidbody>();

            if (m_body == null)
            {
                Debug.LogWarning($"No rigidbody found on {ModuleOwner}, instancing default one.");
                m_body = ModuleOwner.gameObject.AddComponent<Rigidbody>();
            }
        }

        public override void ApplyVelocity(Vector3 newVelocity, float deltaTime)
        {
            if (m_body.isKinematic)
            {
                m_body.position += newVelocity * deltaTime;
                Velocity = Vector3.zero;
            }
            else
            {
                m_body.velocity = newVelocity;
            }
        }
    }

}
