using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterGravityVelocity : CharacterVelocityModule
    {
        [SerializeField]
        private float m_maxFreeFallSpeed = 100f;
        [SerializeField, Range(0, 1)]
        private float m_gravityAcceleration = 0.5f;

#if UNITY_EDITOR
        [SerializeField, ReadOnly]
#endif
        private float m_verticalVelocity = 0;

        public override void StateUpdate(bool grounded)
        {
            if (!grounded)
            {
                return;
            }

            m_verticalVelocity = 0;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            m_verticalVelocity += m_gravityAcceleration * Physics.gravity.y * deltaTime;
            m_verticalVelocity = Mathf.Max(m_verticalVelocity, -m_maxFreeFallSpeed);

            return currentVel + Vector3.up * m_verticalVelocity;
        }

        public override void OnVelocityUpdateCancelled()
        {
            m_verticalVelocity = 0;
        }
    }
}