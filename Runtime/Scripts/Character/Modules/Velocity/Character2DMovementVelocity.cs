using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: 2D Movement")]
    public class Character2DMovementVelocity : CharacterVelocityModuleBase
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

        public enum VelocityClampingOption
        {
            None = 0,
            ClampWhenCharacterVelocityIsZero,
            ClampToSmallerAbsoluteCharacterVelocity,
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
        private VelocityClampingOption m_internalVelocityClamping = VelocityClampingOption.ClampWhenCharacterVelocityIsZero;

        [SerializeField]
        private VelocityProcessing m_accelerationApplication = VelocityProcessing.FromRawInput;

        [ShowIf("DisplayAcelerationFields")]
        [SerializeField, Range(0.01f, 1f)]
        private float m_accelerationTimeInSeconds = .25f;

        [ShowIf("DisplayAcelerationFields")]
        [SerializeField, Range(0.01f, 1f)]
        private float m_decelerationTimeInSeconds = .25f;

        [ShowIf("DisplayDesiredVelocityFields")]
        [SerializeField, Range(0f, 100f)]
        private float m_desiredVelocityMaxAcceleration = 50.0f;

        private Vector3 m_movementVector;

        [SerializeField, ReadOnly]
        private Vector3 m_velocity;

        [SerializeField]
        private bool m_ignoreThirdAxis = false;

#if UNITY_EDITOR

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

        public void SetMovementMaxSpeed(float speed)
        {
            m_maxSpeed = speed;
        }

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
            // Clamp internal velocity based on the last frame final character velocity.
            // For instance, this allows to reset the internal velocity of the module when hitting a wall.
            switch (m_internalVelocityClamping)
            {
                case VelocityClampingOption.ClampToSmallerAbsoluteCharacterVelocity:
                    if (Mathf.Abs(currentVel.x) < Mathf.Abs(m_velocity.x))
                    {
                        m_velocity.x = currentVel.x;
                    }
                    if (Mathf.Abs(currentVel.y) < Mathf.Abs(m_velocity.y))
                    {
                        m_velocity.y = currentVel.y;
                    }
                    if (Mathf.Abs(currentVel.z) < Mathf.Abs(m_velocity.z))
                    {
                        m_velocity.z = currentVel.z;
                    }
                    break;

                case VelocityClampingOption.ClampWhenCharacterVelocityIsZero:
                    if (Mathf.Approximately(currentVel.x, 0))
                    {
                        m_velocity.x = 0;
                    }
                    if (Mathf.Approximately(currentVel.y, 0))
                    {
                        m_velocity.y = 0;
                    }
                    if (Mathf.Approximately(currentVel.z, 0))
                    {
                        m_velocity.z = 0;
                    }
                    break;

                default:
                    break;
            }

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
                            return m_velocity;
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

            if (m_ignoreThirdAxis)
            {
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
            }

            m_movementVector = Vector3.zero;

            m_velocity = Vector3.ClampMagnitude(m_velocity, m_maxSpeed);

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