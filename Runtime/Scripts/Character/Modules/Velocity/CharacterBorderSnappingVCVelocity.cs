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
    ///     - DecelerationForce: the force at which the character decelerates afterMag reaching back the platform
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

    [AddComponentMenu("NobunAtelier/Character/VelocityModule Border Snapping VC")]
    public class CharacterBorderSnappingVCVelocity : CharacterVelocityModuleBase
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

        [SerializeField, Range(0, 1000f)]
        private float m_snapForceDecceleration = 10f;

        [SerializeField]
        private AnimationCurve m_snapAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField, Range(0, 10f)]
        private float m_maxSnapDistance = 3f;

        [SerializeField]
        private AnimationCurve m_snapDeccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        private bool m_clampAcceleration = true;

        [SerializeField]
        private bool m_clampVelocity = true;

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
        private Vector3 m_lastFrameVelocity;

        [SerializeField, Range(0, 1)]
        [Tooltip("From what percentage of alignment the input velocity is projected to the wall. [0 to 1] * 100%")]
        private float m_velocityAlignment = 0.5f;

        [SerializeField, Range(0, 1)]
        private float m_debugTimeScale = 1;

        private Vector3 m_latestClosestPoint = Vector3.zero;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(m_castOrigin != null, $"{this} need a Ray origin transform!");
            if (m_castOrigin == null)
            {
                this.enabled = false;
            }
        }

        [SerializeField]
        private float snapDeccellMaxDuration = 1f;

        [SerializeField]
        private float snapDeccellDuration = 0f;

        private bool m_isSnapping = false;
        private bool m_firstFrameDeccel = false;
        private Vector3 m_surplus;

        public override Vector3 VelocityUpdate(Vector3 externalVelocity, float deltaTime)
        {
            Time.timeScale = m_debugTimeScale;
            Vector3 position = m_castOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_lastHitCollider = hitinfo.collider;

                if (m_snapAcceleration != Vector3.zero)
                {
                    m_snapAcceleration = (m_snapVelocity) / deltaTime;
                    m_snapAcceleration.y = 0;

                    var prevMag = m_snapAcceleration.sqrMagnitude;
                    m_snapAcceleration -= m_snapAcceleration.normalized * m_maxSpeed;
                    var afterMag = m_snapAcceleration.sqrMagnitude;

                    if (afterMag > prevMag)
                    {
                        m_snapAcceleration = Vector3.zero;
                    }
                }
            }
            else if (m_lastHitCollider)
            {

                //if (PhysicsHelper.GetClosestPointFromPenetration(m_lastHitCollider, m_detectionCollider, ref m_latestClosestPoint, out Vector3 normal))
                //{
                //    Debug.DrawLine(m_latestClosestPoint, m_detectionCollider.transform.position, Color.white);
                //    Debug.DrawRay(m_latestClosestPoint, normal, Color.red);
                //}

                m_latestClosestPoint = m_lastHitCollider.ClosestPoint(position);
                Vector3 direction = m_latestClosestPoint - position;
                direction.y = 0;

                m_snapDistanceFactor = direction.sqrMagnitude / (m_maxSnapDistance * m_maxSnapDistance);
                Vector3 snapForce = direction.normalized * m_snapAccelerationCurve.Evaluate(m_snapDistanceFactor) * m_SnapForce;
                m_snapAcceleration += snapForce;
            }

            m_snapVelocity = (m_snapAcceleration * deltaTime);
            ClampVelocity();

            var final = m_snapVelocity + externalVelocity;

            return final;
        }

        public Vector3 VelocityUpdateBackup(Vector3 currentVel, float deltaTime)
        {
            Time.timeScale = m_debugTimeScale;
            // Check if the character is leaving the platform
            Vector3 position = m_castOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_isSnapping = false;
                m_lastHitCollider = hitinfo.collider;
                // Reset snap velocity and acceleration when on platform
                if (m_snapAcceleration != Vector3.zero)
                {
                    snapDeccellDuration += deltaTime;
                    if (m_firstFrameDeccel)
                    {
                        m_firstFrameDeccel = false;
                        // Sopme idea but bad implementation...
                        m_surplus = m_snapAcceleration - m_lastFrameVelocity;
                    }
                    else
                    {
                        m_snapAcceleration = m_lastFrameVelocity;//.normalized * m_snapAcceleration.magnitude;
                        var externalForce = currentVel;
                        externalForce.y = 0f;

                        var estimatedMaxFrameAccel = (m_snapAcceleration + externalForce);

                        var prevMag = m_snapAcceleration.sqrMagnitude;

                        if (m_lastFrameVelocity.sqrMagnitude < estimatedMaxFrameAccel.sqrMagnitude)
                        {
                            //if (Mathf.Abs(m_snapAcceleration.x) > Mathf.Abs(m_lastFrameVelocity.x + externalForce.x))
                            //{
                            //    m_snapAcceleration.x = m_lastFrameVelocity.x + externalForce.x;
                            //}
                            //if (Mathf.Abs(m_snapAcceleration.z) > Mathf.Abs(m_lastFrameVelocity.z + externalForce.z))
                            //{
                            //    m_snapAcceleration.z = m_lastFrameVelocity.z + externalForce.z;
                            //}

                            // Work fine when acceleration is only on one axes but doesn't work weel when acceleartion is going on 2 axes or more
                            //
                            // m_snapAcceleration = Vector3.ClampMagnitude(m_snapAcceleration, m_lastFrameVelocity.magnitude + externalForce.magnitude);
                        }

                        m_snapAcceleration -= m_snapAcceleration.normalized * m_snapForceDecceleration * deltaTime;
                        var afterMag = m_snapAcceleration.sqrMagnitude;

                        var ration = snapDeccellDuration / snapDeccellMaxDuration;
                        currentVel *= 1 - m_snapDeccelerationCurve.Evaluate(ration);

                        if (afterMag > prevMag)
                        {
                            m_snapAcceleration = Vector3.zero;
                        }
                    }
                }
            }
            else if (m_lastHitCollider)
            {
                snapDeccellDuration = 0;
                m_firstFrameDeccel = true;

                m_isSnapping = true;

                if (PhysicsHelper.GetClosestPointFromPenetration(m_lastHitCollider, m_detectionCollider, ref m_latestClosestPoint, out Vector3 normal))
                {
                    Debug.DrawLine(m_latestClosestPoint, m_detectionCollider.transform.position, Color.white);
                    Debug.DrawRay(m_latestClosestPoint, normal, Color.red);
                }

                Vector3 direction = m_latestClosestPoint - position;
                direction.y = 0;

                // Calculates frame snap acceleration
                m_snapDistanceFactor = direction.sqrMagnitude / (m_maxSnapDistance * m_maxSnapDistance);
                Vector3 snapForce = direction.normalized * m_snapAccelerationCurve.Evaluate(m_snapDistanceFactor) * m_SnapForce;
                m_snapAcceleration += snapForce * deltaTime;
                ClampAcceleration();
            }

            m_snapVelocity = m_snapAcceleration + currentVel;
            ClampVelocity();

            m_lastFrameVelocity = m_snapVelocity;
            m_lastFrameVelocity.y = 0;

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
            if (!m_clampVelocity)
            {
                return;
            }

            if (m_snapVelocity.sqrMagnitude > m_maxSpeed * m_maxSpeed)
            {
                // Debug.Log("Clamping velocity!");
                m_snapVelocity = Vector3.ClampMagnitude(m_snapVelocity, m_maxSpeed);
            }
        }

        private void ClampAcceleration()
        {
            if (!m_clampAcceleration)
            {
                return;
            }

            if (m_snapAcceleration.sqrMagnitude > m_maxSnapAcceleration * m_maxSnapAcceleration)
            {
                // Debug.Log("Clamping acceleration!");
                m_snapAcceleration = Vector3.ClampMagnitude(m_snapAcceleration, m_maxSnapAcceleration);
            }
        }

        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || !m_lastHitCollider || !m_isSnapping)
            {
                return;
            }

            Gizmos.color = Color.Lerp(Color.white, Color.red, m_snapDistanceFactor);

            // Vector3 closestPoint = m_lastHitCollider.ClosestPoint(ModuleOwner.Position);

            Gizmos.DrawSphere(ModuleOwner.Position, 0.1f);
            Gizmos.DrawLine(m_latestClosestPoint, ModuleOwner.Position);
            Gizmos.DrawWireSphere(m_latestClosestPoint, 0.5f);
        }

        [SerializeField]
        private SphereCollider m_detectionCollider;

        //public void ColliderCheck()
        //{
        //    if (!m_detectionCollider)
        //        return; // nothing to do without a Collider attached

        //    for (int i = 0; i < maxNeighbours; ++i)
        //        neighbours[i] = null;

        //    int count = Physics.OverlapSphereNonAlloc(transform.position, m_detectionCollider.radius, neighbours, m_groundLayer);

        //    for (int i = 0; i < count; ++i)
        //    {
        //        var collider = neighbours[i];

        //        if (collider == m_detectionCollider)
        //            continue; // skip ourself

        //        if (GetClosestPointFromPenetration(collider, m_detectionCollider, out Vector3 closestPoint, out Vector3 normal))
        //        {
        //            Debug.DrawLine(closestPoint, m_detectionCollider.transform.position, Color.white);
        //            Debug.DrawRay(closestPoint, normal, Color.red);
        //        }
        //    }
        //}
    }

    public static class PhysicsHelper
    {
        public static bool GetClosestPointFromPenetration(Collider target_collider, SphereCollider sphere_collider, ref Vector3 closest_point, out Vector3 surface_normal)
        {
            surface_normal = Vector3.zero;

            Vector3 sphere_pos = sphere_collider.transform.position;
            if (Physics.ComputePenetration(target_collider, target_collider.transform.position, target_collider.transform.rotation, sphere_collider, sphere_pos, Quaternion.identity, out surface_normal, out float surface_penetration_depth))
            {
                closest_point = sphere_pos + (surface_normal * (sphere_collider.radius - surface_penetration_depth));
                surface_normal = -surface_normal * surface_penetration_depth;

                return true;
            }

            return false;
        }
    }
}