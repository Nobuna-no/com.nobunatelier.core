using NaughtyAttributes;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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
        private MovementAxes m_movementAxes = MovementAxes.XZ;

        [ShowIf("DisplayCustomMovementAxisFields")]
        public Vector3 CustomForwardAxis = Vector3.forward;

        [ShowIf("DisplayCustomMovementAxisFields")]
        public Vector3 CustomRightAxis = Vector3.right;

        [SerializeField, Range(0, 100f)]
        private float m_dashDistance = 3.0f;

        [SerializeField, Range(0, 3f)]
        private float m_dashDuration = 1.0f;

        [SerializeField]
        private float m_delayBeforeTwoDashes = 0.1f;

        [SerializeField]
        private float m_temporaryDashDisableDuration = 1.0f;

        [SerializeField]
        private AnimationCurve m_dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 m_movementVector;

        [SerializeField, ReadOnly]
        private Vector3 m_velocity;

        [SerializeField]
        private LayerMask m_blockerLayer;

        [SerializeField]
        private float m_minimalDistanceWithBlocker = 1f;

        private bool m_isDashing = false;
        private float m_currentDashTime = 0;

        private Vector3 m_origin;
        private Vector3 m_destination;
        private bool m_canDash = true;
        private bool m_isFirstFrame = false;
        private bool m_isDashCancelled = false;

        [SerializeField]
        private float m_forwardThreshold = 90f;

        public DashEvent OnDashEvent;

#if UNITY_EDITOR

        [SerializeField]
        private bool m_debug = false;

        private bool DisplayCustomMovementAxisFields()
        {
            return m_movementAxes == MovementAxes.Custom;
        }

#endif

        public void SetActiveDash(bool enable)
        {
            m_canDash = enable;

            if (!m_canDash)
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
            m_canDash = false;
            yield return new WaitForSeconds(m_temporaryDashDisableDuration);
            m_canDash = true;
        }

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
        }

        // Thanks ChatGPT
        private void SendDashDirectionEvent()
        {
#if UNITY_EDITOR
            if (m_debug)
            {
                Debug.DrawRay(ModuleOwner.Position, ModuleOwner.Transform.forward * 2, Color.red, 2);
                Debug.DrawRay(ModuleOwner.Position, m_movementVector, Color.blue, 2);
            }
#endif

            float angle = Vector3.SignedAngle(ModuleOwner.Transform.forward, m_movementVector, Vector3.up);

            // Classify the direction based on the angle.
            if (angle < -m_forwardThreshold || angle > m_forwardThreshold)
            {
                // Move backward
                OnDashEvent?.Invoke(DashDirection.Backward);
            }
            else if (angle < -90f + m_forwardThreshold && angle > -90f - m_forwardThreshold)
            {
                // Move left
                OnDashEvent?.Invoke(DashDirection.Left);
            }
            else if (angle < 90f + m_forwardThreshold && angle > 90f - m_forwardThreshold)
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
            if (!m_canDash || direction == Vector3.zero || m_isDashing || m_currentDashTime > 0)
            {
                return;
            }

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

            m_origin = ModuleOwner.Position;
            m_destination = m_origin + m_movementVector * m_dashDistance;

            Ray ray = new Ray(m_origin, m_movementVector * m_dashDistance);
            // If there is any blocker, we can't dash here
            if (Physics.Raycast(ray, out RaycastHit hitInfo, m_dashDistance + m_minimalDistanceWithBlocker, m_blockerLayer))
            {
                float distance = Vector3.Distance(m_origin, hitInfo.transform.position);

                if (distance <= m_minimalDistanceWithBlocker * 1.2f)
                {
                    // Debug.Log($"There is {hitInfo.collider.gameObject.name} in front of me! Distance: {distance}" +
                    // $"\norigin: {m_origin} and destination: {m_destination}");
                    return;
                }
                else
                {
                    m_destination = hitInfo.transform.position - (hitInfo.transform.position - m_origin).normalized * m_minimalDistanceWithBlocker;
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
            m_currentDashTime = 0;
            m_isDashing = true;
            m_isFirstFrame = true;
            SendDashDirectionEvent();
        }

        private void StopDash()
        {
            if (m_isDashing)
            {
                m_isDashCancelled = true;
            }
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            if (!m_isDashing)
            {
                m_currentDashTime -= deltaTime;
                return currentVel;
            }

            if (m_isDashCancelled)
            {
                m_isDashCancelled = false;
                currentVel -= m_velocity;
                ResetDash();
                return currentVel;
            }

            if (m_isFirstFrame)
            {
                m_isFirstFrame = false;
                currentVel = Vector3.zero;
            }

            m_currentDashTime += deltaTime;
            currentVel -= m_velocity;
            if (m_currentDashTime > m_dashDuration)
            {
                ResetDash();
            }
            else
            {
                Vector3 frameDest = Vector3.Lerp(m_origin, m_destination, m_dashCurve.Evaluate(m_currentDashTime / m_dashDuration));
                Vector3 frameDistance = frameDest - ModuleOwner.Position;
                frameDistance.y = 0;
                m_velocity = frameDistance / deltaTime;
                currentVel += m_velocity;
            }

            m_movementVector = Vector3.zero;

            return currentVel;
        }

        private void ResetDash()
        {
            m_velocity = Vector3.zero;
            m_isDashing = false;
            m_currentDashTime = m_delayBeforeTwoDashes;
        }
    }
}