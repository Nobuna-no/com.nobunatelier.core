using NaughtyAttributes;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: 2D Dash")]
    public class Character2DDashVelocity : CharacterVelocityModuleBase
    {
        [System.Serializable]
        public class DashEvent : UnityEvent<DashDirection>
        { };

        public enum DashDirection
        {
            Forward,
            Backward,
            Left,
            Right
        }

        public enum MovementAxes
        {
            XZ,
            XY,
            YZ,
            Custom
        }

        [SerializeField]
        [FormerlySerializedAs("m_movementAxes")]
        private MovementAxes m_MovementAxes = MovementAxes.XZ;

        [ShowIf("DisplayCustomMovementAxisFields")]
        [FormerlySerializedAs("CustomForwardAxis")]
        public Vector3 m_CustomForwardAxis = Vector3.forward;

        [ShowIf("DisplayCustomMovementAxisFields")]
        [FormerlySerializedAs("CustomRightAxis")]
        public Vector3 m_CustomRightAxis = Vector3.right;

        [SerializeField, Range(0, 100f)]
        [FormerlySerializedAs("m_dashDistance")]
        private float m_DashDistance = 3.0f;

        [SerializeField, Range(0, 3f)]
        [FormerlySerializedAs("m_dashDuration")]
        private float m_DashDuration = 1.0f;

        [SerializeField]
        [FormerlySerializedAs("m_delayBeforeTwoDashes")]
        private float m_DelayBeforeTwoDashes = 0.1f;

        [SerializeField]
        [FormerlySerializedAs("m_temporaryDashDisableDuration")]
        private float m_TemporaryDashDisableDuration = 1.0f;

        [SerializeField]
        [FormerlySerializedAs("m_dashCurve")]
        private AnimationCurve m_DashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 m_MovementVector;

        [SerializeField, ReadOnly]
        [FormerlySerializedAs("m_velocity")]
        private Vector3 m_Velocity;

        [SerializeField]
        [FormerlySerializedAs("m_blockerLayer")]
        private LayerMask m_BlockerLayer;

        [SerializeField]
        [FormerlySerializedAs("m_minimalDistanceWithBlocker")]
        private float m_MinimalDistanceWithBlocker = 1f;

        private bool m_IsDashing = false;
        private float m_CurrentDashTime = 0;

        private Vector3 m_Origin;
        private Vector3 m_Destination;
        private bool m_CanDash = true;
        private bool m_IsFirstFrame = false;
        private bool m_IsDashCancelled = false;

        [SerializeField]
        [FormerlySerializedAs("m_forwardThreshold")]
        private float m_ForwardThreshold = 90f;

        public DashEvent OnDashEvent;

#if UNITY_EDITOR

        [SerializeField]
        [FormerlySerializedAs("m_debug")]
        private bool m_Debug = false;

        private bool DisplayCustomMovementAxisFields()
        {
            return m_MovementAxes == MovementAxes.Custom;
        }

#endif

        public void SetActiveDash(bool enable)
        {
            m_CanDash = enable;

            if (!m_CanDash)
            {
                StopDash();
            }
        }

        public void TemporaryDashDisabling()
        {
            //if (!m_canDash)
            //{
            //    return;
            //}

            StopDash();
            StartCoroutine(DashDisabling_Coroutine());
        }

        private IEnumerator DashDisabling_Coroutine()
        {
            m_CanDash = false;
            yield return new WaitForSeconds(m_TemporaryDashDisableDuration);
            m_CanDash = true;
        }

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
        }

        // Thanks ChatGPT
        private void SendDashDirectionEvent()
        {
#if UNITY_EDITOR
            if (m_Debug)
            {
                Debug.DrawRay(ModuleOwner.Position, ModuleOwner.Transform.forward * 2, Color.red, 2);
                Debug.DrawRay(ModuleOwner.Position, m_MovementVector, Color.blue, 2);
            }
#endif

            float angle = Vector3.SignedAngle(ModuleOwner.Transform.forward, m_MovementVector, Vector3.up);

            // Classify the direction based on the angle.
            if (angle < -m_ForwardThreshold || angle > m_ForwardThreshold)
            {
                // Move backward
                OnDashEvent?.Invoke(DashDirection.Backward);
            }
            else if (angle < -90f + m_ForwardThreshold && angle > -90f - m_ForwardThreshold)
            {
                // Move left
                OnDashEvent?.Invoke(DashDirection.Left);
            }
            else if (angle < 90f + m_ForwardThreshold && angle > 90f - m_ForwardThreshold)
            {
                // Move right
                OnDashEvent?.Invoke(DashDirection.Right);
            }
            else
            {
                OnDashEvent?.Invoke(DashDirection.Forward);
            }
        }

        public override void MoveInput(Vector3 direction)
        {
            // if is currently dashing or if the delay timer is still ongoing...
            if (!m_CanDash || direction == Vector3.zero || m_IsDashing || m_CurrentDashTime > 0)
            {
                return;
            }

            switch (m_MovementAxes)
            {
                case MovementAxes.XZ:
                    m_MovementVector = direction;
                    m_MovementVector.y = 0;
                    break;

                case MovementAxes.XY:
                    m_MovementVector = new Vector3(direction.x, direction.z, 0);
                    break;

                case MovementAxes.YZ:
                    m_MovementVector = new Vector3(0, direction.z, direction.x);
                    break;

                case MovementAxes.Custom:
                    m_MovementVector = m_CustomRightAxis * direction.x + m_CustomForwardAxis * direction.z;
                    break;
            }

            m_MovementVector.Normalize();

            m_Origin = ModuleOwner.Position;
            m_Destination = m_Origin + m_MovementVector * m_DashDistance;

            Ray ray = new Ray(m_Origin, m_MovementVector * m_DashDistance);
            // If there is any blocker, we can't dash here
            if (Physics.Raycast(ray, out RaycastHit hitInfo, m_DashDistance + m_MinimalDistanceWithBlocker, m_BlockerLayer))
            {
                float distance = Vector3.Distance(m_Origin, hitInfo.transform.position);

                if (distance <= m_MinimalDistanceWithBlocker * 1.2f)
                {
                    // Debug.Log($"There is {hitInfo.collider.gameObject.name} in front of me! Distance: {distance}" +
                    // $"\norigin: {m_origin} and destination: {m_destination}");
                    return;
                }
                else
                {
                    m_Destination = hitInfo.transform.position - (hitInfo.transform.position - m_Origin).normalized * m_MinimalDistanceWithBlocker;
                    StartDash();
                }
            }
            else
            {
                StartDash();
            }
        }

        private void StartDash()
        {
            m_CurrentDashTime = 0;
            m_IsDashing = true;
            m_IsFirstFrame = true;
            SendDashDirectionEvent();
        }

        private void StopDash()
        {
            if (m_IsDashing)
            {
                m_IsDashCancelled = true;
            }
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            if (!m_IsDashing)
            {
                m_CurrentDashTime -= deltaTime;
                return currentVel;
            }

            if (m_IsDashCancelled)
            {
                m_IsDashCancelled = false;
                currentVel -= m_Velocity;
                ResetDash();
                return currentVel;
            }

            if (m_IsFirstFrame)
            {
                m_IsFirstFrame = false;
                currentVel = Vector3.zero;
            }

            m_CurrentDashTime += deltaTime;
            currentVel -= m_Velocity;
            if (m_CurrentDashTime > m_DashDuration)
            {
                ResetDash();
            }
            else
            {
                Vector3 frameDest = Vector3.Lerp(m_Origin, m_Destination, m_DashCurve.Evaluate(m_CurrentDashTime / m_DashDuration));
                Vector3 frameDistance = frameDest - ModuleOwner.Position;
                frameDistance.y = 0;
                m_Velocity = frameDistance / deltaTime;
                currentVel += m_Velocity;
            }

            m_MovementVector = Vector3.zero;

            return currentVel;
        }

        private void ResetDash()
        {
            m_Velocity = Vector3.zero;
            m_IsDashing = false;
            m_CurrentDashTime = m_DelayBeforeTwoDashes;
        }
    }
}