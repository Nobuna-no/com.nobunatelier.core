using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterUnityCharacterController : CharacterBodyModuleBase
    {
        [SerializeField]
        private bool m_useSimpleMove = false;

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
