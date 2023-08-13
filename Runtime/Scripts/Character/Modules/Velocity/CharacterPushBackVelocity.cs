using NaughtyAttributes;
using NobunAtelier.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Velocity/VelocityModule: Push Back")]
    public class CharacterPushBackVelocity : CharacterVelocityModuleBase
    {
        //TO DO:
        // public enum MovementAxes
        // {
        //     XZ,
        //     XY,
        //     YZ,
        // }
        //private MovementAxes m_movementAxes = MovementAxes.XZ;

        [SerializeField]
        private bool m_ForwardZ = true;
        [SerializeField]
        private bool m_useAttackerPositionInsteadOfImpactPosition = false;

        private float m_currentTime = 0;
        private ProceduralMovementDefinition m_pushBack;

        private HealthBehaviour m_healthComponent;
        private Vector3 m_destination = Vector3.zero;
        private Vector3 m_origin = Vector3.zero;

        private bool m_isPushingBack = false;

        [SerializeField, ReadOnly]
        private Vector3 m_velocity;

        public void HitPush(HitInfo info)
        {
            if (info.Hit == null || info.Hit.PushBackDefinition == null)
            {
                // Debug.LogWarning($"Can't use {this} with null HitDefinition.");
                return;
            }

            m_origin = ModuleOwner.Position;
            m_pushBack = info.Hit.PushBackDefinition;
            m_currentTime = 0;
            Vector3 coord1 = (m_origin - (m_useAttackerPositionInsteadOfImpactPosition ? info.Origin.transform.position : info.ImpactLocation));
            coord1.y = 0;
            coord1.Normalize();
            Vector3 coord2 = new Vector3(-coord1.z, 0, coord1.x);
            Vector3 xCord = (m_ForwardZ ? m_pushBack.MovementUnit.z : m_pushBack.MovementUnit.x) * coord1;
            Vector3 zCord = (m_ForwardZ ? m_pushBack.MovementUnit.x : m_pushBack.MovementUnit.z) * coord2;
            Vector3 totalMovement = xCord + zCord;

            m_destination = ModuleOwner.Position + totalMovement;
            m_isPushingBack = true;
            m_velocity = Vector3.zero;
            // Debug.DrawLine(info.ImpactLocation, transform.position, Color.green, 1f);
            Debug.DrawLine(m_origin, m_destination, Color.red, m_pushBack.DurationInSeconds);
        }

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            Debug.Assert(character.TryGetAbilityModule(out m_healthComponent), "Fail to get HealthBehaviour", this);

            if (m_healthComponent != null)
            {
                m_healthComponent.OnHit.AddListener(HitPush);
                // m_healthComponent.OnDeath.AddListener(HitPush);
            }
        }

        public override bool CanBeExecuted()
        {
            return base.CanBeExecuted() && m_isPushingBack;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            m_currentTime += deltaTime;
            currentVel -= m_velocity;
            if (m_currentTime > m_pushBack.DurationInSeconds)
            {
                ResetDash();
            }
            else
            {
                Vector3 frameDest = Vector3.Lerp(m_origin, m_destination, m_pushBack.MovementAnimationCurve.Evaluate(m_currentTime / m_pushBack.DurationInSeconds));
                Vector3 frameDistance = frameDest - ModuleOwner.Position;
                frameDistance.y = 0;
                m_velocity = frameDistance / deltaTime;
                currentVel += m_velocity;
            }

            if (m_currentTime > 1f)
            {
                m_isPushingBack = false;
                currentVel = Vector3.zero;
            }

            return currentVel;
        }

        private void ResetDash()
        {
            m_velocity = Vector3.zero;
            m_isPushingBack = false;
            m_currentTime = 0;
        }
    }
}
