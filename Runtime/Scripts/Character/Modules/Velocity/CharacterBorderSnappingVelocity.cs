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
    ///     - SnappingControl: How much control can the player have while being snapped? Not sure if that should be a m_duration or based on the distance...
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

    [AddComponentMenu("NobunAtelier/Character/VelocityModule Border Snapping")]
    public class CharacterBorderSnappingVelocity : CharacterVelocityModuleBase
    {
        [SerializeField]
        private LayerMask m_groundLayer;

        [SerializeField]
        private Transform m_castOrigin;

        [SerializeField]
        private float m_rayCastMaxDistance = 0.5f;

        [SerializeField, Range(0, 100f)]
        private float m_maxSpeed = 30f;

        [SerializeField]
        private bool m_clampSpeed = true;

        [SerializeField, Range(1f, 100f)]
        private float m_snapForceAcceleration = 25f;

        [SerializeField, Range(0.1f, 10f)]
        private float m_snapForceDecceleration = 1;

        [SerializeField]
        private AnimationCurve m_snapAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField, Range(0, 10f)]
        private float m_maxSnapDistance = 3f;

        [SerializeField, Range(0, 10f)]
        private float m_maxSnapDuration = 3f;

        private Collider m_lastHitCollider;
        private Vector3 m_snapAcceleration = Vector3.zero;
        private Vector3 m_snapVelocity = Vector3.zero;
        private Vector3 m_latestClosestPoint = Vector3.zero;
        private float m_snapDistanceFactor = 0;

        [SerializeField, ReadOnly]
        private float m_snapDuration = 0;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(m_castOrigin != null, $"{this} need a Ray origin transform!");
            if (m_castOrigin == null)
            {
                this.enabled = false;
            }
        }

        public override Vector3 VelocityUpdate(Vector3 externalVelocity, float deltaTime)
        {
            Vector3 position = m_castOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_lastHitCollider = hitinfo.collider;
                m_snapDuration = 0;
                if (m_snapAcceleration != Vector3.zero)
                {
                    m_snapAcceleration = (m_snapVelocity) / deltaTime;
                    m_snapAcceleration.y = 0;

                    var prevMag = m_snapAcceleration.sqrMagnitude;
                    m_snapAcceleration -= m_snapAcceleration.normalized * m_snapForceDecceleration * m_maxSpeed;
                    var afterMag = m_snapAcceleration.sqrMagnitude;

                    if (afterMag > prevMag)
                    {
                        m_snapAcceleration = Vector3.zero;
                        m_latestClosestPoint = Vector3.zero;
                    }
                }
            }
            else if (m_lastHitCollider)
            {
                m_latestClosestPoint = m_lastHitCollider.ClosestPoint(position);
                Vector3 direction = m_latestClosestPoint - position;
                direction.y = 0;

                m_snapDistanceFactor = direction.sqrMagnitude / (m_maxSnapDistance * m_maxSnapDistance);
                // The intention is: the more we are near the maxSnapDistance the more we reach the max m_duration
                m_snapDuration += deltaTime + (m_maxSnapDuration * m_snapDistanceFactor);
                float overflow = m_snapDuration - m_maxSnapDuration;
                Vector3 snapForce = direction.normalized * m_snapAccelerationCurve.Evaluate(m_snapDuration / m_maxSnapDuration) * m_snapForceAcceleration;
                m_snapAcceleration += snapForce + (snapForce * overflow * deltaTime);
            }

            m_snapVelocity = (m_snapAcceleration * deltaTime);
            ClampVelocity();

            var final = m_snapVelocity + externalVelocity;

            return final;
        }

        private void ClampVelocity()
        {
            if (!m_clampSpeed)
            {
                return;
            }

            if (m_snapVelocity.sqrMagnitude > m_maxSpeed * m_maxSpeed)
            {
                m_snapVelocity = Vector3.ClampMagnitude(m_snapVelocity, m_maxSpeed);
            }
        }

        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || !m_lastHitCollider || m_latestClosestPoint.sqrMagnitude == 0)
            {
                return;
            }

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(ModuleOwner.Position, 0.1f);

            Gizmos.color = Color.Lerp(Color.white, Color.red, m_snapDistanceFactor);
            Gizmos.DrawLine(m_latestClosestPoint, ModuleOwner.Position);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_latestClosestPoint, 0.5f);
        }
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