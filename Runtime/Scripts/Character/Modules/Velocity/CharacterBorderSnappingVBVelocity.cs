using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Goal:
    /// -
    /// - When the character is leaving a platform, it will gradually be "snapped" back toward the closest border using a cumulative snap acceleration
    /// - While snapped, when it reaches the border, it will decelerate gradually until stopped (when the snap acceleration reaches zero)
    /// - The goal is to have control over:
    ///     - MaxBorderDistance: the further the distance, the stronger the SnappingForce is applied. This value is the distance at which the maximum snappingForce is applied
    ///     - SnappingForce: the force at which the character is snapped toward the nearest border
    ///     - SnappingControl: How much control can the player have while being snapped? Not sure if that should be a duration or based on the distance...
    ///     - DecelerationForce: the force at which the character decelerates after reaching back the platform
    ///     - DecelerationControl: How much control can the player have while decelerating.
    /// </summary>

    // if m_currentControl == 1: convert all the external vel toward the snapping (which is bad as it means that even trying to go back
    // will lead to increase velocity...
    // What should happens:
    //      - the cumulative acceleration should take over the velocity just like gravity would...
    //          - But then does that mean we have a physically based movement?
    //          - how about control? How to induce more control over the snapping?
    //              - I think that control should only be aligned to the snapping
    //                  - But then that would mean no longer have the option to move to the right and back?
    // In the end I think that the lerp should be directly affecting snapAcceleration to currentVel...


    [AddComponentMenu("NobunAtelier/Character/VelocityModule Border Snapping VB")]
    public class CharacterBorderSnappingVBVelocity : CharacterVelocityModuleBase
    {
        [SerializeField] private LayerMask m_groundLayer;
        [SerializeField] private Transform m_castOrigin;
        [SerializeField] private float m_rayCastMaxDistance = 1f;

        [SerializeField, Range(0, 100f)]
        private float m_SnapForce = 25f;
        [SerializeField, Range(0, 100f)]
        private float m_maxSnapAcceleration = 25f;
        [SerializeField, Range(0, 100f)]
        private float m_maxSpeed = 50f;
        [SerializeField, Range(0, 100f)]
        private float m_snapForceDecceleration = 10f;
        [SerializeField]
        private AnimationCurve m_snapAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Range(0, 10f)]
        private float m_maxSnapDistance = 3f;

        [Header("Control")]
        [SerializeField, Range(0f, 10f)]
        private float m_snappingAccelerationControlCurveDuration = 1f;
        [SerializeField]
        private AnimationCurve m_controlOverSnappingAccelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);


        [Header("Debug")]
        [SerializeField, ReadOnly]
        private Collider m_lastHitCollider;

        [SerializeField, ReadOnly]
        private Vector3 m_snapAcceleration = Vector3.zero;
        [SerializeField, ReadOnly]
        private Vector3 m_snapVelocity = Vector3.zero;
        [SerializeField, ReadOnly]
        private float m_currentControlRatio = 0;
        [SerializeField, ReadOnly]
        private float m_snappingDuration = 0f;
        [SerializeField, ReadOnly]
        private float m_snapDistanceFactor = 0;
        [SerializeField, ReadOnly]
        private float m_snapAccelerationMagnitude = 0;
        [SerializeField, ReadOnly]
        Vector3 updatedVelocity;

        [SerializeField, Range(0, 1)]
        [Tooltip("From what percentage of alignment the input velocity is projected to the wall. [0 to 1] * 100%")]
        private float m_velocityAlignment = 0.5f;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(m_castOrigin != null, $"{this} need a Ray origin transform!");
            if (m_castOrigin == null)
            {
                this.enabled = false;
            }
        }
        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            // Check if the character is leaving the platform
            Vector3 position = m_castOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_lastHitCollider = hitinfo.collider;

                // Reset snap velocity and acceleration when on platform
                m_snapAcceleration = Vector3.zero;
                m_snapAccelerationMagnitude = 0f;
                m_currentControlRatio = 1f;
                m_snappingDuration = 0f;
                m_snapVelocity = Vector3.zero;

                return currentVel;
            }

            m_snappingDuration += deltaTime;

            Vector3 closestPoint = m_lastHitCollider.ClosestPoint(position);
            Vector3 direction = closestPoint - position;
            direction.y = 0;

            // Calculates frame snap acceleration
            m_snapDistanceFactor = direction.sqrMagnitude / (m_maxSnapDistance * m_maxSnapDistance);
            Vector3 snapForce = direction.normalized * m_snapAccelerationCurve.Evaluate(m_snapDistanceFactor) * m_SnapForce;
            m_snapAcceleration += snapForce * deltaTime;
            ClampAcceleration();

            // If no external forces, returns external force + snap force
            updatedVelocity = new Vector3(currentVel.x, 0, currentVel.z);
            if (updatedVelocity.sqrMagnitude <= 0.1f)
            {
                m_snapVelocity = currentVel + m_snapAcceleration;
                ClampVelocity();
                return m_snapVelocity;
            }

            // Else, calculate external force influence
            m_currentControlRatio = m_controlOverSnappingAccelerationCurve.Evaluate(m_snappingDuration / m_snappingAccelerationControlCurveDuration);
            m_snapAccelerationMagnitude = m_snapAcceleration.magnitude;

            Vector3 movementDir = updatedVelocity.normalized;
            float movementBorderDotProduct = Vector3.Dot(movementDir, m_snapAcceleration.normalized);
            if (movementBorderDotProduct <= -m_velocityAlignment)
            {
                // If the movement direction is opposed to the wall normal, cancel out the velocity along the axis perpendicular to the wall.
                float axisDiff = Mathf.Abs(movementDir.x) - Mathf.Abs(movementDir.z);
                bool isAxisDiffWithinEpsilon = Mathf.Abs(axisDiff) < m_velocityAlignment;

                updatedVelocity.x = isAxisDiffWithinEpsilon ? 0 : (axisDiff > 0 ? 0 : updatedVelocity.x);
                updatedVelocity.z = isAxisDiffWithinEpsilon ? 0 : (axisDiff > 0 ? updatedVelocity.z : 0);
                Debug.Log($"Alignment[{movementBorderDotProduct}], updatedVelocity[{updatedVelocity}]");
            }
            //else
            //{
            //    // Calculate the dot product between the movement direction and the snap acceleration direction
            //    float dotProduct = Vector3.Dot(movementDir.normalized, m_snapAcceleration.normalized);

            //    // Calculate the magnitudes of the updated velocity along the x and z axes
            //    float xMag = Mathf.Abs(updatedVelocity.x) * Mathf.Abs(dotProduct);
            //    float zMag = Mathf.Abs(updatedVelocity.z) * Mathf.Abs(dotProduct);

            //    // Calculate the signs of the updated velocity along the x and z axes
            //    float xSign = Mathf.Sign(updatedVelocity.x) * Mathf.Sign(dotProduct);
            //    float zSign = Mathf.Sign(updatedVelocity.z) * Mathf.Sign(dotProduct);

            //    // Update the updated velocity with the magnitudes and signs calculated above
            //    updatedVelocity.x = xMag * xSign;
            //    updatedVelocity.z = zMag * zSign;
            //    Debug.Log($"Aligned and updated [{movementBorderDotProduct}]: {updatedVelocity}");
            //}

            // This is the available external force in the di
            var orientedVelocity = m_snapAcceleration.normalized * updatedVelocity.magnitude;
            // Based on current control ration, takes certain amount of external force into account.
            var finalExternalVelocity = Vector3.Lerp(orientedVelocity, currentVel, m_currentControlRatio);

            m_snapVelocity = finalExternalVelocity + m_snapAcceleration;

            return m_snapVelocity;
        }

        public static Vector3 RoundDirection(Vector3 v, float epsilon)
        {
            return new Vector3(
                Mathf.Round(v.x / epsilon) * epsilon,
                Mathf.Round(v.y / epsilon) * epsilon,
                Mathf.Round(v.z / epsilon) * epsilon
            );
        }

        private void ClampVelocity()
        {
            if (m_snapVelocity.sqrMagnitude > m_maxSpeed * m_maxSpeed)
            {
                Debug.Log("Clamping velocity!");
                m_snapVelocity = Vector3.ClampMagnitude(m_snapVelocity, m_maxSpeed);
            }
        }

        private void ClampAcceleration()
        {
            if (m_snapAcceleration.sqrMagnitude > m_maxSnapAcceleration * m_maxSnapAcceleration)
            {
                Debug.Log("Clamping acceleration!");
                m_snapAcceleration = Vector3.ClampMagnitude(m_snapAcceleration, m_maxSnapAcceleration);
            }
        }

        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || !m_lastHitCollider)
            {
                return;
            }

            Gizmos.color = Color.Lerp(Color.white, Color.red, m_snapDistanceFactor);

            Vector3 closestPoint = m_lastHitCollider.ClosestPoint(ModuleOwner.Position);

            Gizmos.DrawSphere(ModuleOwner.Position, 0.1f);
            Gizmos.DrawLine(closestPoint, ModuleOwner.Position);
            Gizmos.DrawWireSphere(closestPoint, 0.5f);
        }
    }
}