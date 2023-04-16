using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterMovementModule_2AxisMovement : CharacterMovementModule
    {
        [SerializeField]
        private float m_moveSpeed;
        [SerializeField]
        private float m_rotationSpeed;

        [SerializeField, ReadOnly]
        protected Vector3 m_lastMoveVector;

        // 1. Evaluate current active state
        // 2. Feed input
        // 3. Update velocity?

        // issue with this:
        // - Module_Movement
        // - Module_Rotation
        // - Module_Animation

        public bool EvaluateState()
        {
            return true;
        }

        public void MovementInput(Vector3 normalizedDirection)
        {
            m_lastMoveVector = normalizedDirection;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            return currentVelocity;
        }
    }
}
