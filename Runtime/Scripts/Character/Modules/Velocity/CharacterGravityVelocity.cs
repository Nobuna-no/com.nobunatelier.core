using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    // Better use the rigidbody.gravity instead...
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: Gravity")]
    public class CharacterGravityVelocity : CharacterVelocityModuleBase
    {
        [SerializeField]
        private float m_maxFreeFallSpeed = 100f;

        [SerializeField, Range(0, 10)]
        private float m_gravityMultiplier = 1f;

        [SerializeField, ReadOnly]
        private bool m_isGrounded;

        public override void StateUpdate(bool grounded)
        {
            m_isGrounded = grounded;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            if (m_isGrounded)
            {
                currentVel.y = 0;
            }

            Vector3 finalVelocity = currentVel + Vector3.up * Physics.gravity.y * m_gravityMultiplier * deltaTime;
            finalVelocity.y = Mathf.Max(finalVelocity.y, -m_maxFreeFallSpeed);

            return finalVelocity;
        }
    }
}