using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/VelocityModule Border Slide")]
    public class CharacterBorderSlideVelocity : CharacterVelocityModuleBase
    {
        [SerializeField]
        private LayerMask m_groundLayer;

        [SerializeField]
        private SphereCollider m_detectionCollider;

        [SerializeField]
        private Transform m_rayOrigin;

        [SerializeField, Range(0.01f, 10f)]
        private float m_rayCastMaxDistance = 0.2f;

        [SerializeField, Range(0, 1)]
        [Tooltip("From what percentage of alignment the input velocity is projected to the wall. [0 to 1] * 100%")]
        private float m_velocityAlignment = 0.98f;

        [SerializeField, Range(0f, 10f)]
        [Tooltip("Distance from which we start to apply snapping")]
        private float m_borderSnapDistance = 0.1f;

        [SerializeField, Range(0, 100)]
        [Tooltip("The force applied when snapping to the border.")]
        private float m_snappingForce = 10f;

        [SerializeField, Range(0, 100f)]
        private float m_maxSnappingVelocity = 20f;

        private Collider m_lastGroundCollider;
        private Vector3 m_lastClosestPoint = Vector3.zero;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(m_rayOrigin != null);
            Debug.Assert(m_detectionCollider != null);
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            Vector3 position = m_rayOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_lastGroundCollider = hitinfo.collider;
                return currentVel;
            }
            else if (m_lastGroundCollider)
            {
                Vector3 updatedVelocity = currentVel;
                updatedVelocity.y = 0;
                Vector3 movementDir = new Vector3(currentVel.x, 0, currentVel.z).normalized;

                Vector3 closestPoint = m_lastGroundCollider.ClosestPoint(position);
                Vector3 borderNormal = closestPoint - position;
                float borderDistance = borderNormal.sqrMagnitude;
                borderNormal.y = 0;
                borderNormal.Normalize();

                Vector3 slideDir = Vector3.ProjectOnPlane(movementDir, borderNormal).normalized;

                // Calculates the degree of parallelism.
                float movementBorderDotProduct = Vector3.Dot(movementDir, borderNormal);
                if (movementBorderDotProduct <= -m_velocityAlignment)
                {
                    // If the movement direction is opposed to the wall normal, cancel out the velocity along the axis perpendicular to the wall.
                    float axisDiff = Mathf.Abs(movementDir.x) - Mathf.Abs(movementDir.z);
                    bool isAxisDiffWithinEpsilon = Mathf.Abs(axisDiff) < m_velocityAlignment;
                    updatedVelocity.x = isAxisDiffWithinEpsilon ? 0 : (axisDiff > 0 ? updatedVelocity.x : 0);
                    updatedVelocity.z = isAxisDiffWithinEpsilon ? 0 : (axisDiff > 0 ? 0 : updatedVelocity.z);
                }
                else if (movementBorderDotProduct > 0f)
                {
                    // If the movement direction is aligned with the wall normal, keep the current velocity.
                    return currentVel;
                }

                slideDir *= updatedVelocity.magnitude;

                // If the player is too far away from the border, snap them towards the border.
                if (borderDistance > m_borderSnapDistance * m_borderSnapDistance)
                {
                    slideDir += borderNormal * borderDistance * m_snappingForce;

                    // If the player's velocity exceeds the maximum speed after snapping, clamp it.
                    if (slideDir.sqrMagnitude > m_maxSnappingVelocity * m_maxSnappingVelocity)
                    {
                        slideDir = Vector3.ClampMagnitude(slideDir, m_maxSnappingVelocity);
                    }
                }

                // Add the remaining y velocity from the input velocity as it was already clamped before sliding.
                // The maximum speed only accounts for the snapping force from the slide direction.
                slideDir.y = currentVel.y;
                return slideDir;
            }

            return currentVel;
        }
    }
}