using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    using Gameplay;

    // For top down 3D only
    [RequireComponent(typeof(LegacyCharacterBase))]
    public class HitPushBackBehaviour : MonoBehaviour
    {
        [SerializeField]
        private bool m_ForwardZ = true;
        [SerializeField]
        private bool m_useAttackerPositionInsteadOfImpactPosition = false;

        private LegacyCharacterBase m_characterMovement;
        private HealthBehaviour m_healthComponent;
        private Vector3 m_destination = Vector3.zero;
        private Vector3 m_origin = Vector3.zero;
        [SerializeField, ReadOnly, Foldout("Debug")]
        private float m_currentTime = 0;

        [SerializeField, ReadOnly, Foldout("Debug")]
        private ProceduralMovementDefinition m_pushBack;

        public void HitPush(HitInfo info)
        {
            if (info.Hit == null || info.Hit.PushBackDefinition == null)
            {
                // Debug.LogWarning($"Can't use {this} with null HitDefinition.");
                return;
            }

            m_origin = m_characterMovement.Position;
            m_pushBack = info.Hit.PushBackDefinition;
            m_currentTime = 0;

            Vector3 attackOrigin = info.ImpactLocation;
            if (m_useAttackerPositionInsteadOfImpactPosition)
            {
                attackOrigin = info.OriginTeam ? info.OriginTeam.ModuleOwner.Position : (info.OriginGao ? info.OriginGao.transform.position : info.ImpactLocation);
            }

            Vector3 coord1 = m_origin - attackOrigin;
            coord1.y = 0;
            coord1.Normalize();
            Vector3 coord2 = new Vector3(-coord1.z, 0, coord1.x);
            Vector3 xCord = (m_ForwardZ ? m_pushBack.MovementUnit.z : m_pushBack.MovementUnit.x) * coord1;
            Vector3 zCord = (m_ForwardZ ? m_pushBack.MovementUnit.x : m_pushBack.MovementUnit.z) * coord2;
            Vector3 totalMovement = xCord + zCord;

            m_destination = m_characterMovement.Position + totalMovement;
            this.enabled = true;

            // Debug.DrawLine(info.ImpactLocation, transform.position, Color.green, 1f);
            Debug.DrawLine(m_origin, m_destination, Color.red, m_pushBack.DurationInSeconds);
        }

        private void Awake()
        {
            m_characterMovement = GetComponent<LegacyCharacterBase>();
            m_healthComponent = GetComponent<HealthBehaviour>();
            this.enabled = false;

            if (m_healthComponent != null)
            {
                m_healthComponent.OnHit.AddListener(HitPush);
                m_healthComponent.OnDeath.AddListener(HitPush);
            }
        }

        private void FixedUpdate()
        {
            m_currentTime += Time.fixedDeltaTime;
            if (m_currentTime > 1f)
            {
                this.enabled = false;
                return;
            }

            float progression = m_pushBack.MovementAnimationCurve.Evaluate(m_currentTime / m_pushBack.DurationInSeconds);
            Vector3 dest = Vector3.Lerp(m_origin, m_destination, progression);
            var deltaMove = dest - m_characterMovement.Position;

            m_characterMovement.ProceduralMove(deltaMove);
        }
    }
}