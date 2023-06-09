using Cinemachine;
using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Velocity module
    /// When the character is leaving a platform, it will gradually be "snapped" back toward the closest border using a cumulative snap acceleration
    /// While snapped, when it reaches the border, it will decelerate gradually until stopped (when the snap acceleration reaches zero)
    /// The goal is to have control over:
    ///     - MaxBorderDistance: the further the distance, the stronger the SnappingForce is applied. This value is the distance at which the maximum snappingForce is applied
    ///     - SnappingForce: the force at which the character is snapped toward the nearest border
    ///     - SnappingControl: How much control can the player have while being snapped? Not sure if that should be a duration or based on the distance...
    ///     - DecelerationForce: the force at which the character decelerates after reaching back the platform
    ///     - DecelerationControl: How much control can the player have while decelerating.
    /// </summary>


    [AddComponentMenu("NobunAtelier/Character/VelocityModule Border Snapping")]
    public class CharacterBorderSnappingVelocity : CharacterVelocityModuleBase
    {
        public enum SnappingAccelerationType
        {
            Acceleration,
            Duration
        }

        [Header("Physics")]
        [SerializeField]
        private LayerMask m_groundLayer;
        [SerializeField]
        private Transform m_castOrigin;
        [SerializeField, Range(0.01f, 10f)]
        private float m_rayCastMaxDistance = 1f;

        [Header("Snap")]
        [SerializeField]
        private SnappingAccelerationType m_accelerationType;
        [SerializeField, Range(0, 100f)]
        private float m_maxSnapAcceleration = 25f;
        [SerializeField, Range(0, 100f)]
        private float m_maxSpeed = 50f;
        [SerializeField, Range(0, 100f)]
        private float m_snapForceDecceleration = 10f;
        [SerializeField]
        private AnimationCurve m_snapAccelerationCurve = AnimationCurve.EaseInOut(0,0,1,1);
        [SerializeField, Range(0, 10f)]
        private float m_maxSnapDistance = 3f;

        [Header("Control")]
        [SerializeField, Range(0f, 10f)]
        private float m_snappingAccelerationControlCurveDuration = 1f;
        [SerializeField]
        private AnimationCurve m_controlOverSnappingAccelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);


        [SerializeField, Range(0f, 10f)]
        private float m_snappingDeccelerationControlCurveDuration = 1f;
        [SerializeField]
        private AnimationCurve m_controlOverSnappingDeccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);


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

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(m_castOrigin != null, $"{this} need a Ray origin transform!");
            if (m_castOrigin == null)
            {
                this.enabled = false;
            }
        }
        [SerializeField, ReadOnly]
        private Vector3 movementDir;
        [SerializeField, ReadOnly]
        private Vector3 m_lastMovementDir;
        [SerializeField]
        private bool m_newDecel = true;
        [SerializeField]
        private float m_debugTimeScale = 1;

        [SerializeField, Range(0, 1)]
        private float m_velocityAlignment = 0.98f;
        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            Vector3 position = m_castOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_lastHitCollider = hitinfo.collider;

                if (m_snappingDuration > 0)
                {
                    m_snappingDuration = 0;
                    m_lastMovementDir = m_snapAcceleration.normalized;
                }
                else
                {
                    m_snappingDuration -= deltaTime;
                }

                if (m_snapAcceleration == Vector3.zero)
                {
                    return currentVel;
                }

                UnityEngine.Time.timeScale = m_debugTimeScale;


                if (!m_newDecel)
                {

                    if (m_accelerationType == SnappingAccelerationType.Acceleration)
                    {
                        m_snapAcceleration = Vector3.MoveTowards(m_snapAcceleration, Vector3.zero, m_snapForceDecceleration * deltaTime);
                    }
                    else
                    {
                        m_snapAcceleration = Vector3.Lerp(m_lastSnapAcell, Vector3.zero, -m_snappingDuration / m_snapForceDecceleration);
                    }

                    ClampAcceleration();


                    if (currentVel.x == 0f && currentVel.z == 0)
                    {
                        m_snapVelocity = currentVel + m_lastMovementDir * m_snapAcceleration.magnitude;
                        ClampVelocity();
                        return m_snapVelocity;
                    }


                    {
                        m_snapAccelerationMagnitude = m_snapAcceleration.magnitude;
                        var orientedDeccel = currentVel.normalized * m_snapAcceleration.magnitude;
                        m_currentControlRatio = m_controlOverSnappingDeccelerationCurve.Evaluate(-m_snappingDuration / m_snappingDeccelerationControlCurveDuration);
                        // m_snapVelocity = currentVel + Vector3.Lerp(m_snapAcceleration, orientedDeccel, m_currentControlRatio);


                        // var orrientedVelDeccel = m_snapAcceleration.normalized * currentVel.magnitude;
                        // var velOverrideDecelControl = m_externalVelocitySnappingOverrideCurve.Evaluate(-m_snappingDuration / m_snappingDeccelerationControlCurveDuration);
                        // m_snapVelocity = Vector3.Lerp(orrientedVelDeccel, currentVel, m_currentControlRatio) + Vector3.Lerp(m_snapAcceleration, orientedDeccel, m_currentControlRatio);

                        var testOrrientedVelDec = m_snapAcceleration.normalized * currentVel.magnitude;
                        var testControlDec = m_currentControlRatio;
                        var testVelocityDDec = Vector3.Lerp(testOrrientedVelDec, currentVel, testControlDec);

                        m_snapVelocity = testVelocityDDec + m_snapAcceleration;
                        m_lastMovementDir = m_snapVelocity.normalized;
                    }

                    // Debug.DrawRay(ModuleOwner.Position, m_snapVelocity.normalized * 3, Color.green, 0.5f);
                    // Debug.DrawRay(ModuleOwner.Position, testVelocityDDec.normalized * 3, Color.white, 0.5f);
                    // Debug.DrawRay(ModuleOwner.Position, currentVel.normalized * 2, Color.cyan, 0.5f);
                    // Debug.DrawRay(ModuleOwner.Position, testOrrientedVelDec.normalized * 2, Color.yellow, 0.5f);

                    ClampVelocity();
                }
                else
                {
                    Vector3 workingVelocity = currentVel;
                    workingVelocity.y = 0;

                    Vector3 decelForce = m_snapAcceleration.normalized * /*m_controlOverSnappingDeccelerationCurve.Evaluate(-m_snappingDuration / m_snappingDeccelerationControlCurveDuration) **/ m_snapForceDecceleration;

                    float initialSquareMag = m_snapAcceleration.sqrMagnitude;
                    m_snapAcceleration -= decelForce * deltaTime;
                    if (m_snapAcceleration.sqrMagnitude >= initialSquareMag)
                    {
                        m_snapAcceleration = Vector3.zero;
                        return currentVel;
                    }

                    ClampAcceleration();

                    if (currentVel.x == 0f && currentVel.z == 0)
                    {
                        m_snapVelocity = currentVel + m_snapAcceleration;
                        ClampVelocity();
                        return m_snapVelocity;
                    }

                    m_currentControlRatio = m_controlOverSnappingDeccelerationCurve.Evaluate(-m_snappingDuration / m_snappingDeccelerationControlCurveDuration);

                    //m_snapAccelerationMagnitude = m_snapAcceleration.magnitude;

                    //float movementToSnapDotProduct = Vector3.Dot(workingVelocity, m_snapAcceleration);
                    //if (movementToSnapDotProduct <= -m_velocityAlignment)
                    //{
                    //    float axisDiff = Mathf.Abs(movementDir.x) - Mathf.Abs(movementDir.z);
                    //    bool isAxisDiffWithinEpsilon = Mathf.Abs(axisDiff) < m_velocityAlignment;
                    //    workingVelocity.x = isAxisDiffWithinEpsilon ? 0 : (axisDiff > 0 ? workingVelocity.x : 0);
                    //    workingVelocity.z = isAxisDiffWithinEpsilon ? 0 : (axisDiff > 0 ? 0 : workingVelocity.z);
                    //}

                    Vector3 orientedInputVelocity = m_snapAcceleration.normalized * workingVelocity.magnitude;

                    // float controlOverSnappingDeceleration = m_currentControlRatio;
                    // 0 = We orient all the input velocity toward the snap
                    // 1 = We directly take the working velocity
                    Vector3 finalControlledVelocity = Vector3.Lerp(orientedInputVelocity, workingVelocity, m_currentControlRatio);

                    // Ok find the issue, only the facing velocity should be added! just like with the borderSlide

                    // The issue here is that we need to add the input to the final velocity
                    m_snapVelocity = finalControlledVelocity + m_snapAcceleration;
                    m_snapAcceleration = m_snapVelocity.normalized * m_snapAcceleration.magnitude;
                    //m_lastMovementDir = m_snapVelocity.normalized;
                    ClampVelocity();

                    m_snapVelocity.y += currentVel.y;
                }
                return m_snapVelocity;
            }

            if (m_snappingDuration < 0)
            {
                m_snappingDuration = 0;
            }
            m_snappingDuration += deltaTime;

            Vector3 closestPoint = m_lastHitCollider.ClosestPoint(position);
            Vector3 direction = closestPoint - position;
            direction.y = 0;

            m_snapDistanceFactor = direction.sqrMagnitude / (m_maxSnapDistance * m_maxSnapDistance);
            Vector3 snapForce = direction.normalized * m_snapAccelerationCurve.Evaluate(m_snapDistanceFactor) * m_maxSnapAcceleration;

            m_snapAcceleration += snapForce * deltaTime;
            ClampAcceleration();


            // var diff = snapDir + movementDir;
            // Debug.Log($"movement + snap = {diff} - sqrMagnitude = {diff.sqrMagnitude}");
            // var slideDir = diff.sqrMagnitude < 0.1f ? Vector3.zero : new Vector3(-snapDir.z, 0, snapDir.x);
            // Debug.Log($"slideDir = {slideDir}");
            // slideDir.x *= Sign(movementDir.x);
            // slideDir.z *= Sign(movementDir.z);
            // Debug.Log($"slideDir = {slideDir}");

            // slideDir = slideDir.normalized * currentVel.magnitude;

            //if (m_slideTowardMovement)
            //{
            //    var snapDir = m_snapAcceleration.normalized;
            //    movementDir = new Vector3(currentVel.x, 0, currentVel.z);
            //    // movementDir = RoundDirection(movementDir.normalized, m_epsilonRound);

            //    Vector3 slideDir = movementDir;
            //    if (slideDir.x != 0 && Mathf.Sign(snapDir.x) != Mathf.Sign(movementDir.x))
            //    {
            //        slideDir.x = snapDir.z * Mathf.Sign(movementDir.x);
            //    }

            //    if (slideDir.z != 0 && Mathf.Sign(snapDir.z) != Mathf.Sign(movementDir.z))
            //    {
            //        slideDir.z = -snapDir.x * Mathf.Sign(movementDir.z);
            //    }
            //    Debug.Log($"Signs: snap/move X({Mathf.Sign(snapDir.x)} / {Mathf.Sign(movementDir.x)}({movementDir.x})) Z({Mathf.Sign(snapDir.z)} / {Mathf.Sign(movementDir.z)}({movementDir.z}))");
            //    Debug.Log($"slideDir: {slideDir} / currentVel = {currentVel} ");


            //    slideDir.Normalize();
            //    slideDir.x *= Mathf.Abs(currentVel.x);
            //    slideDir.y = currentVel.y;
            //    slideDir.z *= Mathf.Abs(currentVel.z);

            //    Debug.DrawRay(ModuleOwner.Position, slideDir.normalized * 3, Color.white, 0.5f); // Color.Lerp(Color.red, Color.yellow, velOverrideControl)
            //    Debug.DrawRay(ModuleOwner.Position, movementDir * 2, Color.cyan, 0.5f);
            //    Debug.DrawRay(ModuleOwner.Position, snapDir * 2, Color.yellow, 0.5f);

            //    return slideDir;
            //}

            if (currentVel.x == 0f && currentVel.z == 0)
            {
                m_snapVelocity = currentVel + m_snapAcceleration;
                ClampVelocity();
                return m_snapVelocity;
            }

            m_snapAccelerationMagnitude = m_snapAcceleration.magnitude;
            // var orientedAccel = currentVel.normalized * m_snapAcceleration.magnitude;
            m_currentControlRatio = m_controlOverSnappingAccelerationCurve.Evaluate(m_snappingDuration / m_snappingAccelerationControlCurveDuration);

            // Ok c'est cool d'avoir un moyen de dire a quel point le joueur affecte le snap, mais la velocite actuel du joeur a quand
            // meme un impact sur le movement final, meme avec 0 control. Ici ) control signifie que le joueur n'a aucun control sur le snap,
            // mais cela veut pas dire que les force exterieur n'ont pas de controle sur le snap.
            // Pour permettre cela, il faut que la currentVel puissent etre affecter par le snap aussi...
            // m_snapVelocity = currentVel + Vector3.Lerp(m_snapAcceleration, orientedAccel, m_currentControlRatio);

            var testOrrientedVel = m_snapAcceleration.normalized * currentVel.magnitude;
            var testControl = m_currentControlRatio;
            var testVelocity = Vector3.Lerp(testOrrientedVel, currentVel, testControl);
            m_snapVelocity = testVelocity + m_snapAcceleration;// , orientedAccel, m_currentControlRatio);

            // var orrientedVel = m_snapAcceleration.normalized * currentVel.magnitude;
            // var velOverrideControl = m_externalVelocitySnappingOverrideCurve.Evaluate(m_snappingDuration / m_snappingAccelerationControlCurveDuration);
            // var finalExternalVelocity = Vector3.Lerp(orrientedVel, currentVel, velOverrideControl);
            // m_snapVelocity = finalExternalVelocity  + Vector3.Lerp(orrientedVel, currentVel, velOverrideControl) + Vector3.Lerp(m_snapAcceleration, orientedAccel, m_currentControlRatio);

            Debug.DrawRay(ModuleOwner.Position, testVelocity.normalized * 3, Color.white, 0.5f); // Color.Lerp(Color.red, Color.yellow, velOverrideControl)
            Debug.DrawRay(ModuleOwner.Position, m_snapVelocity.normalized * 3, Color.green, 0.5f);
            Debug.DrawRay(ModuleOwner.Position, currentVel.normalized * 2, Color.cyan, 0.5f);
            Debug.DrawRay(ModuleOwner.Position, testOrrientedVel.normalized * 2, Color.yellow, 0.5f);

            // Debug.Log($"orrientedVel({velOverrideControl}): " + finalExternalVelocity);

            ClampVelocity();
            m_lastSnapAcell = m_snapAcceleration;
            return m_snapVelocity;
        }
        [SerializeField, ReadOnly]
        Vector3 m_lastSnapAcell;
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