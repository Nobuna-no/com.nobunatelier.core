using Cinemachine;
using Codice.Client.Common;
using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/VelocityModule Snap To Border")]
    public class CharacterSnapToBorderVelocity : CharacterVelocityModuleBase
    {
        [Header("Physics")]
        [SerializeField]
        private LayerMask m_groundLayer;
        [SerializeField]
        private Transform m_castOrigin;
        [SerializeField, Range(0.01f, 10f)]
        private float m_rayCastMaxDistance = 1f;

        [Header("Snap")]
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
        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            Vector3 position = m_castOrigin.position;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hitinfo, m_rayCastMaxDistance, m_groundLayer))
            {
                m_lastHitCollider = hitinfo.collider;

                if (m_snappingDuration > 0)
                {
                    m_snappingDuration = 0;
                }

                if (m_snapAcceleration == Vector3.zero)
                {
                    return currentVel;
                }


                m_snappingDuration -= deltaTime;

                m_snapAcceleration = Vector3.MoveTowards(m_snapAcceleration, Vector3.zero, m_snapForceDecceleration * deltaTime);
                ClampAcceleration();

                if (currentVel.x == 0f && currentVel.z == 0)
                {
                    m_snapVelocity = currentVel + m_snapAcceleration;
                    ClampVelocity();
                    return m_snapVelocity;
                }

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

                // Debug.DrawRay(ModuleOwner.Position, m_snapVelocity.normalized * 3, Color.green, 0.5f);
                // Debug.DrawRay(ModuleOwner.Position, testVelocityDDec.normalized * 3, Color.white, 0.5f);
                // Debug.DrawRay(ModuleOwner.Position, currentVel.normalized * 2, Color.cyan, 0.5f);
                // Debug.DrawRay(ModuleOwner.Position, testOrrientedVelDec.normalized * 2, Color.yellow, 0.5f);

                ClampVelocity();

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