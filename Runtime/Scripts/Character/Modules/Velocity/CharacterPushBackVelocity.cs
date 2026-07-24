using NaughtyAttributes;
using NobunAtelier.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: Push Back")]
    public class CharacterPushBackVelocity : CharacterVelocityModuleBase
    {
        private enum PushBackPhase
        {
            None,
            Push,
            WallBounce
        }

        [SerializeField]
        private bool m_ForwardZ = true;

        [SerializeField]
        private bool m_useAttackerPositionInsteadOfImpactPosition = false;

        [Tooltip("When enabled, XZ velocity from lower-priority modules is cleared while pushback is active.")]
        [SerializeField]
        private bool m_OverridePlanarVelocity = true;

        [Tooltip("When enabled, applies MovementUnit.y from the pushback definition (world up).")]
        [SerializeField]
        private bool m_IncludeVertical;

        [Header("Wall bounce")]
        [SerializeField]
        private bool m_WallBounceEnabled = true;

        [SerializeField, Min(0f)]
        private float m_BounceDistance = 0.35f;

        [SerializeField, Min(0.01f)]
        private float m_BounceDuration = 0.1f;

        [SerializeField, Range(0f, 0.35f)]
        private float m_BounceOvershoot = 0.12f;

        [SerializeField, Range(0.05f, 1f)]
        private float m_BlockDetectionRatio = 0.35f;

        [SerializeField, Range(0f, 1f)]
        private float m_BounceRemainingPushScale = 0.5f;

        [SerializeField]
        private LayerMask m_ObstacleLayers = ~0;

        [SerializeField, Min(0.1f)]
        private float m_WallNormalRayHeight = 0.75f;

        private float m_currentTime;
        private float m_bounceTime;
        private ProceduralMovementDefinition m_pushBack;

        private HealthBehaviour m_healthComponent;
        private Vector3 m_destination = Vector3.zero;
        private Vector3 m_origin = Vector3.zero;
        private Vector3 m_pushPlanarDirection = Vector3.zero;
        private Vector3 m_bounceOrigin = Vector3.zero;
        private Vector3 m_bounceDestination = Vector3.zero;

        private PushBackPhase m_phase = PushBackPhase.None;
        private bool m_isPushingBack;

        [SerializeField, ReadOnly]
        private Vector3 m_velocity;

        public UnityEvent OnPushBackBegin;
        public UnityEvent OnPushBackEnd;
        public UnityEvent OnWallBounce;

        [SerializeField]
        private bool m_logDebug;

        public void HitPush(HitInfo info)
        {
            if (info.Hit == null || info.Hit.PushBackDefinition == null)
            {
                if (m_logDebug)
                {
                    Debug.LogWarning($"Can't use {this} with null HitDefinition.");
                }

                return;
            }

            m_origin = ModuleOwner.Position;
            m_pushBack = info.Hit.PushBackDefinition;
            m_currentTime = 0f;
            m_bounceTime = 0f;
            m_phase = PushBackPhase.Push;

            Vector3 attackOrigin = info.ImpactLocation;
            if (m_useAttackerPositionInsteadOfImpactPosition)
            {
                if (info.OriginTeam && info.OriginTeam.ModuleOwner)
                {
                    attackOrigin = info.OriginTeam.ModuleOwner.Position;
                }
                else if (info.OriginGao)
                {
                    attackOrigin = info.OriginGao.transform.position;
                }
                else
                {
                    attackOrigin = info.ImpactLocation;
                }
            }

            Vector3 coord1 = m_origin - attackOrigin;
            coord1.y = 0;
            if (coord1.sqrMagnitude < 0.0001f && info.OriginTeam != null && info.OriginTeam.ModuleOwner != null)
            {
                coord1 = m_origin - info.OriginTeam.ModuleOwner.Position;
                coord1.y = 0;
            }

            if (coord1.sqrMagnitude < 0.0001f)
            {
                coord1 = ModuleOwner.transform.forward;
                coord1.y = 0;
            }

            coord1.Normalize();
            Vector3 coord2 = new Vector3(-coord1.z, 0, coord1.x);
            Vector3 xCord = (m_ForwardZ ? m_pushBack.MovementUnit.z : m_pushBack.MovementUnit.x) * coord1;
            Vector3 zCord = (m_ForwardZ ? m_pushBack.MovementUnit.x : m_pushBack.MovementUnit.z) * coord2;
            Vector3 totalMovement = xCord + zCord;

            if (m_IncludeVertical)
            {
                totalMovement.y = m_pushBack.MovementUnit.y;
            }

            m_pushPlanarDirection = totalMovement;
            m_pushPlanarDirection.y = 0f;
            if (m_pushPlanarDirection.sqrMagnitude > 0.0001f)
            {
                m_pushPlanarDirection.Normalize();
            }
            else
            {
                m_pushPlanarDirection = coord1;
            }

            m_destination = ModuleOwner.Position + totalMovement;
            m_isPushingBack = true;
            m_velocity = Vector3.zero;
            Debug.DrawLine(m_origin, m_destination, Color.red, m_pushBack.DurationInSeconds);
            OnPushBackBegin?.Invoke();
        }

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            CaptureHealthBehaviour();
        }

        public override bool CanBeExecuted()
        {
            return base.CanBeExecuted() && m_isPushingBack;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            if (m_OverridePlanarVelocity)
            {
                currentVel.x = 0f;
                currentVel.z = 0f;
            }

            switch (m_phase)
            {
                case PushBackPhase.Push:
                    UpdatePushPhase(currentVel, deltaTime);
                    break;
                case PushBackPhase.WallBounce:
                    UpdateWallBouncePhase(deltaTime);
                    break;
            }

            return currentVel;
        }

        private void UpdatePushPhase(Vector3 currentVel, float deltaTime)
        {
            m_currentTime += deltaTime / m_pushBack.DurationInSeconds;
            var normalizedTime = Mathf.Clamp01(m_currentTime);
            var curveValue = m_pushBack.MovementAnimationCurve.Evaluate(normalizedTime);
            var frameDest = Vector3.Lerp(m_origin, m_destination, curveValue);

            m_velocity = ApplyRootMotionToward(
                frameDest,
                deltaTime,
                allowVertical: m_IncludeVertical,
                out var requestedDelta,
                out var moveResult);

            if (m_WallBounceEnabled
                && TryGetWallImpact(requestedDelta, moveResult, out var wallNormal))
            {
                BeginWallBounce(wallNormal);
                return;
            }

            if (m_currentTime > 1f)
            {
                CompletePushBack(ref currentVel);
            }
        }

        private void UpdateWallBouncePhase(float deltaTime)
        {
            m_bounceTime += deltaTime / m_BounceDuration;
            var t = Mathf.Clamp01(m_bounceTime);
            var eased = EvaluateBounceEase(t);
            var frameDest = Vector3.LerpUnclamped(m_bounceOrigin, m_bounceDestination, eased);

            m_velocity = ApplyRootMotionToward(
                frameDest,
                deltaTime,
                allowVertical: false,
                out _,
                out _);

            if (m_bounceTime > 1f)
            {
                FinishPushBack();
            }
        }

        private void CompletePushBack(ref Vector3 currentVel)
        {
            currentVel.x = 0f;
            currentVel.z = 0f;
            FinishPushBack();
        }

        private void FinishPushBack()
        {
            m_velocity = Vector3.zero;
            m_isPushingBack = false;
            m_phase = PushBackPhase.None;
            m_currentTime = 0f;
            m_bounceTime = 0f;
            OnPushBackEnd?.Invoke();
        }

        private void BeginWallBounce(Vector3 wallNormal)
        {
            wallNormal.y = 0f;
            if (wallNormal.sqrMagnitude < 0.0001f)
            {
                wallNormal = -m_pushPlanarDirection;
            }
            else
            {
                wallNormal.Normalize();
            }

            var bounceDirection = Vector3.Reflect(m_pushPlanarDirection, wallNormal);
            bounceDirection.y = 0f;
            if (bounceDirection.sqrMagnitude < 0.0001f)
            {
                bounceDirection = -m_pushPlanarDirection;
            }
            else
            {
                bounceDirection.Normalize();
            }

            var remainingPush = Vector3.Distance(ModuleOwner.Position, m_destination);
            var bounceDistance = Mathf.Min(
                m_BounceDistance,
                Mathf.Max(0.08f, remainingPush * m_BounceRemainingPushScale));

            m_phase = PushBackPhase.WallBounce;
            m_bounceTime = 0f;
            m_bounceOrigin = ModuleOwner.Position;
            m_bounceDestination = m_bounceOrigin + bounceDirection * bounceDistance;

            if (m_logDebug)
            {
                Debug.DrawLine(m_bounceOrigin, m_bounceDestination, Color.yellow, m_BounceDuration);
            }

            OnWallBounce?.Invoke();
        }

        private bool TryGetWallImpact(
            Vector3 requestedDelta,
            CharacterRootMotionMoveResult moveResult,
            out Vector3 wallNormal)
        {
            wallNormal = Vector3.zero;

            var planarRequested = requestedDelta;
            planarRequested.y = 0f;

            var requestedSqr = planarRequested.sqrMagnitude;
            if (requestedSqr < 0.0001f)
            {
                return false;
            }

            var appliedPlanar = moveResult.AppliedDelta;
            appliedPlanar.y = 0f;

            var appliedSqr = appliedPlanar.sqrMagnitude;
            var blockThreshold = requestedSqr * m_BlockDetectionRatio * m_BlockDetectionRatio;
            var blockedByTravel = appliedSqr < blockThreshold;
            var blockedBySides = (moveResult.CollisionFlags & CollisionFlags.Sides) != 0;

            if (!blockedByTravel && !blockedBySides)
            {
                return false;
            }

            var direction = planarRequested.normalized;
            var rayOrigin = ModuleOwner.Position + Vector3.up * m_WallNormalRayHeight;
            var rayDistance = planarRequested.magnitude + 0.5f;

            if (Physics.Raycast(
                    rayOrigin,
                    direction,
                    out var hit,
                    rayDistance,
                    m_ObstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                wallNormal = hit.normal;
                return true;
            }

            wallNormal = -direction;
            return true;
        }

        private Vector3 ApplyRootMotionToward(
            Vector3 worldPosition,
            float deltaTime,
            bool allowVertical,
            out Vector3 requestedDelta,
            out CharacterRootMotionMoveResult moveResult)
        {
            moveResult = default;
            requestedDelta = Vector3.zero;

            if (ModuleOwner.Body == null || deltaTime <= 0f)
            {
                return Vector3.zero;
            }

            requestedDelta = worldPosition - ModuleOwner.Position;
            if (!allowVertical)
            {
                requestedDelta.y = 0f;
            }

            moveResult = ModuleOwner.Body.ApplyRootMotionDeltaDetailed(requestedDelta, includeVertical: allowVertical);
            return moveResult.AppliedDelta / deltaTime;
        }

        private float EvaluateBounceEase(float t)
        {
            var outBack = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            return Mathf.LerpUnclamped(t, outBack, m_BounceOvershoot / 0.12f);
        }

        private void OnEnable()
        {
            if (ModuleOwner == null)
            {
                return;
            }

            CaptureHealthBehaviour();
        }

        private void OnDisable()
        {
            if (m_healthComponent == null)
            {
                return;
            }

            m_healthComponent.OnHit.RemoveListener(HitPush);
            m_healthComponent = null;
        }

        private void CaptureHealthBehaviour()
        {
            if (ModuleOwner.TryGetAbilityModule(out m_healthComponent))
            {
                m_healthComponent.OnHit.AddListener(HitPush);
            }
            else
            {
                Debug.LogError($"[{Time.frameCount}] {this.name}: character doesn't have a {typeof(HealthBehaviour).Name}.", this);
            }
        }
    }
}
