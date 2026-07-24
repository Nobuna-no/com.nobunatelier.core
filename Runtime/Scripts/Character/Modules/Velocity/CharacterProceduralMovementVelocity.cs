using NaughtyAttributes;
using NobunAtelier.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    /// <summary>
    /// Drives character displacement from a <see cref="ProceduralMovementDefinition"/>, timed via
    /// <see cref="BeginMove"/> (e.g. Animancer clip events). Uses character-local axes at move start.
    /// </summary>
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: Procedural Movement")]
    public class CharacterProceduralMovementVelocity : CharacterVelocityModuleBase
    {
        [SerializeField]
        private bool m_ForwardZ = true;

        [Tooltip("When enabled, XZ velocity from lower-priority modules is cleared while this move is active.")]
        [SerializeField]
        private bool m_OverridePlanarVelocity = true;

        [Tooltip("When disabled, vertical displacement from MovementUnit.y is ignored.")]
        [SerializeField]
        private bool m_IncludeVertical;

        [Tooltip("Keeps body rotation captured at BeginMove so look input cannot steer the lunge.")]
        [SerializeField]
        private bool m_LockFacingDuringMove = true;

        [SerializeField, ReadOnly]
        private bool m_IsMoving;

        [SerializeField, ReadOnly]
        private Vector3 m_Velocity;

        public bool IsMoving => m_IsMoving;

        public UnityEvent OnMoveBegin;
        public UnityEvent OnMoveEnd;

        [SerializeField]
        private bool m_LogDebug;

        private float m_CurrentTime;
        private ProceduralMovementDefinition m_Definition;
        private Vector3 m_Origin;
        private Vector3 m_Destination;
        private Quaternion m_FacingRotation;

        public void BeginMove(ProceduralMovementDefinition definition)
        {
            if (definition == null)
            {
                if (m_LogDebug)
                {
                    Debug.LogWarning($"[{nameof(CharacterProceduralMovementVelocity)}] BeginMove called with null definition.", this);
                }

                return;
            }

            if (ModuleOwner == null)
            {
                if (m_LogDebug)
                {
                    Debug.LogWarning($"[{nameof(CharacterProceduralMovementVelocity)}] BeginMove called before ModuleInit.", this);
                }

                return;
            }

            var wasMoving = m_IsMoving;
            m_Origin = ModuleOwner.Position;
            m_FacingRotation = ModuleOwner.Body != null ? ModuleOwner.Body.Rotation : ModuleOwner.transform.rotation;
            m_Definition = definition;
            m_CurrentTime = 0f;
            m_Velocity = Vector3.zero;
            m_Destination = m_Origin + ComputeLocalOffset(definition, ModuleOwner.transform);
            m_IsMoving = true;

            if (m_LogDebug)
            {
                Debug.DrawLine(m_Origin, m_Destination, Color.cyan, definition.DurationInSeconds);
            }

            if (!wasMoving)
            {
                OnMoveBegin?.Invoke();
            }
        }

        public void CancelMove()
        {
            if (!m_IsMoving)
            {
                return;
            }

            EndMove();
        }

        public override bool CanBeExecuted()
        {
            return base.CanBeExecuted() && m_IsMoving;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            if (m_LockFacingDuringMove && ModuleOwner.Body != null)
            {
                ModuleOwner.Body.Rotation = m_FacingRotation;
            }

            if (m_OverridePlanarVelocity)
            {
                currentVel.x = 0f;
                currentVel.z = 0f;
            }

            m_CurrentTime += deltaTime / m_Definition.DurationInSeconds;
            var normalizedTime = Mathf.Clamp01(m_CurrentTime);
            var curveValue = m_Definition.MovementAnimationCurve.Evaluate(normalizedTime);
            var frameDest = Vector3.Lerp(m_Origin, m_Destination, curveValue);

            m_Velocity = ApplyRootMotionToward(frameDest, deltaTime);

            if (m_CurrentTime > 1f)
            {
                CompleteMove(ref currentVel);
            }

            return currentVel;
        }

        private Vector3 ApplyRootMotionToward(Vector3 worldPosition, float deltaTime)
        {
            if (ModuleOwner.Body == null || deltaTime <= 0f)
            {
                return Vector3.zero;
            }

            var delta = worldPosition - ModuleOwner.Position;
            if (!m_IncludeVertical)
            {
                delta.y = 0f;
            }

            var appliedDelta = ModuleOwner.Body.ApplyRootMotionDelta(delta, m_IncludeVertical);
            return appliedDelta / deltaTime;
        }

        private void CompleteMove(ref Vector3 currentVel)
        {
            m_Velocity = Vector3.zero;

            if (m_OverridePlanarVelocity)
            {
                currentVel.x = 0f;
                currentVel.z = 0f;
            }

            EndMove();
        }

        public override void Reset()
        {
            base.Reset();
            EndMove(suppressEndEvent: true);
        }

        private void EndMove(bool suppressEndEvent = false)
        {
            var wasMoving = m_IsMoving;
            m_Velocity = Vector3.zero;
            m_IsMoving = false;
            m_CurrentTime = 0f;
            m_Definition = null;

            if (wasMoving && !suppressEndEvent)
            {
                OnMoveEnd?.Invoke();
            }
        }

        private Vector3 ComputeLocalOffset(ProceduralMovementDefinition definition, Transform characterTransform)
        {
            var forward = characterTransform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var right = Vector3.Cross(Vector3.up, forward);
            var xCord = (m_ForwardZ ? definition.MovementUnit.z : definition.MovementUnit.x) * forward;
            var zCord = (m_ForwardZ ? definition.MovementUnit.x : definition.MovementUnit.z) * right;
            var totalMovement = xCord + zCord;

            if (m_IncludeVertical)
            {
                totalMovement.y = definition.MovementUnit.y;
            }

            return totalMovement;
        }
    }
}
