using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/VelocityModule 2D Movement")]
    public class Character2DMovementVelocity : CharacterVelocityModule
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

        [ShowIf("DisplayCustomMovementAxisFields")]
        public Vector3 CustomForwardAxis = Vector3.forward;

        [ShowIf("DisplayCustomMovementAxisFields")]
        public Vector3 CustomRightAxis = Vector3.right;

        [SerializeField, Range(0, 100f)]
        private float m_maxSpeed = 10.0f;

        [SerializeField]
        private VelocityProcessing m_accelerationApplication = VelocityProcessing.FromRawInput;

        [ShowIf("DisplayAcelerationFields")]
        [SerializeField, Range(0.01f, 1f)]
        private float m_accelerationTimeInSeconds = 10.0f;

        [ShowIf("DisplayAcelerationFields")]
        [SerializeField, Range(0.01f, 1f)]
        private float m_decelerationTimeInSeconds = 10.0f;

        [ShowIf("DisplayDesiredVelocityFields")]
        [SerializeField, Range(0f, 100f)]
        private float m_desiredVelocityMaxAcceleration = 50.0f;

        private Vector3 m_movementVector;
#if UNITY_EDITOR
        [SerializeField, ReadOnly]
        private Vector3 m_velocity;

        private bool DisplayCustomMovementAxisFields()
        {
            return m_movementAxes == MovementAxes.Custom;
        }

        private bool DisplayAcelerationFields()
        {
            return m_accelerationApplication == VelocityProcessing.FromAcceleration;
        }

        private bool DisplayDesiredVelocityFields()
        {
            return m_accelerationApplication == VelocityProcessing.DesiredVelocityFromAcceleration;
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

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            switch (m_accelerationApplication)
            {
                case VelocityProcessing.FromRawInput:
                    m_velocity = m_movementVector * m_maxSpeed;
                    break;

                case VelocityProcessing.FromAcceleration:
                    if (m_movementVector == Vector3.zero)
                    {
                        if (m_velocity == Vector3.zero)
                        {
                            return currentVel;
                        }

                        float previousSqrtMag = m_velocity.sqrMagnitude;
                        Vector3 acceleration = m_velocity.normalized / m_decelerationTimeInSeconds;
                        m_velocity -= acceleration * m_maxSpeed * deltaTime;

                        if (m_velocity.sqrMagnitude > previousSqrtMag)
                        {
                            m_velocity = Vector3.zero;
                        }
                    }
                    else
                    {
                        Vector3 acceleration = m_movementVector / m_accelerationTimeInSeconds;
                        m_velocity += acceleration * m_maxSpeed * deltaTime;
                        m_velocity = Vector3.ClampMagnitude(m_velocity, m_maxSpeed);
                    }
                    break;

                // To move in a new module...
                case VelocityProcessing.DesiredVelocityFromAcceleration:
                    Vector3 desiredVelocity = m_movementVector * m_maxSpeed;
                    float maxSpeedChange = deltaTime * m_desiredVelocityMaxAcceleration;

                    m_velocity = Vector3.MoveTowards(currentVel, desiredVelocity, maxSpeedChange);
                    break;
            }

            var diffVec = Vector3.one - GetMovementSpace();
            if (diffVec.x != 0)
            {
                m_velocity.x = currentVel.x;
            }
            if (diffVec.y != 0)
            {
                m_velocity.y = currentVel.y;
            }
            if (diffVec.z != 0)
            {
                m_velocity.z = currentVel.z;
            }

            m_movementVector = Vector3.zero;

            return m_velocity;
        }

        private Vector3 GetMovementSpace()
        {
            switch (m_movementAxes)
            {
                case MovementAxes.XZ:
                    return new Vector3(1, 0, 1);

                case MovementAxes.XY:
                    return new Vector3(1, 1, 0);

                case MovementAxes.YZ:
                    return new Vector3(0, 1, 1);

                case MovementAxes.Custom:
                    return CustomRightAxis + CustomForwardAxis;
            }

            return Vector3.zero;
        }
    }
}