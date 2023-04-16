using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    public class Character2DVelocity : CharacterVelocityModule
    {
        public enum MovementAxes
        {
            XZ,
            XY,
            YZ,
            Custom
        }

        public enum VelocityProcessing
        {
            // No acceleration, velocity calculated from raw inputDirection
            FromRawInput,
            // Prioritize acceleration control over velocity
            FromAcceleration,
            // Use the acceleration but prioritize velocity control
            DesiredVelocityFromAcceleration,
        }

        [SerializeField]
        private MovementAxes m_movementAxes = MovementAxes.XZ;
        [ShowIf("DisplayCustomMovementAxes")]
        public Vector3 CustomForwardAxis = Vector3.forward;
        [ShowIf("DisplayCustomMovementAxes")]
        public Vector3 CustomRightAxis = Vector3.right;
        [SerializeField]
        private VelocityProcessing m_accelerationApplication = VelocityProcessing.FromRawInput;
        [SerializeField, Range(0, 100f)]
        private float m_maxAcceleration = 10.0f;
        [SerializeField, Range(0, 100f)]
        private float m_maxSpeed = 10.0f;

        private Vector3 m_movementVector;
        private Vector3 m_acceleration;


#if UNITY_EDITOR
        private bool DisplayCustomMovementAxes()
        {
            return m_movementAxes == MovementAxes.Custom;
        }
#endif

        public bool EvaluateState()
        {
            return true;
        }

        public override void MoveInput(Vector3 direction)
        {
            switch (m_movementAxes)
            {
                case MovementAxes.XZ:
                    m_movementVector = direction;
                    m_movementVector.y = 0;
                    break;
                case MovementAxes.XY:
                    m_movementVector = new Vector3(direction.x, direction.z, 0);
                    break;
                case MovementAxes.YZ:
                    m_movementVector = new Vector3(0, direction.z, direction.x);
                    break;
                case MovementAxes.Custom:
                    m_movementVector = CustomRightAxis * direction.x + CustomForwardAxis * direction.z;
                    break;
            }

            m_movementVector.Normalize();
        }

        public override Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            switch (m_accelerationApplication)
            {
                case VelocityProcessing.FromRawInput:
                    currentVelocity += m_movementVector * m_maxSpeed;
                    break;

                case VelocityProcessing.FromAcceleration:
                    Vector3 acceleration = m_movementVector * m_maxAcceleration;
                    currentVelocity += m_acceleration * m_maxSpeed * deltaTime;
                    break;

                case VelocityProcessing.DesiredVelocityFromAcceleration:
                    Vector3 desiredVelocity = m_movementVector * m_maxSpeed;
                    float maxSpeedChange = m_maxAcceleration * deltaTime;
                    currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, maxSpeedChange);
                    break;
            }

            m_movementVector = Vector3.zero;

            return currentVelocity;
        }
    }
}