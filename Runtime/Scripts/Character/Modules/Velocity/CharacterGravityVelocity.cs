using System;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterGravityVelocity : CharacterVelocityModule
    {
        [SerializeField]
        private float m_gravityMultiplier = 1;
        private float m_verticalVelocity = 0;

        private bool m_wasGroundedLastFrame = true;

        public override void StateUpdate(bool grounded)
        {
            if (grounded)
            {
                m_verticalVelocity = 0;
            }

            grounded = m_wasGroundedLastFrame;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            m_verticalVelocity += Physics.gravity.y * m_gravityMultiplier * Time.deltaTime;
            currentVelocity.y += m_verticalVelocity;
            return currentVelocity;
        }
    }
}