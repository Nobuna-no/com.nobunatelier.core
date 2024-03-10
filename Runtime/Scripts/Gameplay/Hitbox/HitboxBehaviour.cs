using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class HitboxBehaviour : MonoBehaviour
    {
        [Header("Hitbox")]
        [SerializeField, Tooltip("The team behind the attack.")]
        private TeamDefinition m_team;

        [SerializeField]
        private TeamDefinition.Target m_target;

        [SerializeField]
        private HitDefinition m_hitDefinition;

        [SerializeField, Tooltip("The team module owner of the hit. Can be use by the hit receiver to track the origin of the attack.")]
        private TeamModule m_hitOriginTeam;

        [SerializeField, Tooltip("The owner of the hit. Can be use by the hit receiver to track the origin of the attack.")]
        private GameObject m_hitOriginGao;

        [SerializeField]
        private Transform m_impactOriginSocket;

        [SerializeField]
        protected float m_damageMultiplier = 1;
        protected Collider OwnCollider => m_collider;
        private Collider m_collider;

        public UnityEvent OnHitboxEnabled;
        public UnityEvent OnHitboxDisabled;
        public HitEvent OnHit;

        public void SetOwner(TeamModule owner, TeamDefinition team = null)
        {
            m_hitOriginTeam = owner;
            m_team = team != null ? team : (owner != null ? owner.Team : null);
        }

        public void SetTargetDefinition(TeamDefinition.Target target)
        {
            m_target = target;
        }

        public void SetHitDefinition(HitDefinition hit)
        {
            m_hitDefinition = hit;
        }

        public virtual void HitBegin()
        {
            m_collider.enabled = true;
            OnHitboxEnabled?.Invoke();
        }

        public virtual void HitEnd()
        {
            m_collider.enabled = false;
            OnHitboxDisabled?.Invoke();
        }

        protected virtual void OnTargetHit()
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryDamageApply(other))
            {
                OnTargetHit();
            }
        }

        protected virtual void Awake()
        {
            m_collider = GetComponent<Collider>();
            m_collider.isTrigger = true;
            HitEnd();
        }

        protected bool TryDamageApply(Collider other)
        {
            if (other == null || m_hitDefinition == null)
            {
                return false;
            }

            var hpBehaviour = other.GetComponent<HealthBehaviour>();

            bool cannotHitSelf = (m_target & TeamDefinition.Target.Self) == 0 && m_hitOriginTeam == other.GetComponent<TeamModule>();
            if (cannotHitSelf || !hpBehaviour || hpBehaviour.IsDead || !m_team.IsTargetValid(m_target, hpBehaviour.Team))
            {
                return false;
            }

            HitInfo info = new HitInfo
            {
                OriginTeam = m_hitOriginTeam ? m_hitOriginTeam : null,
                OriginGao = m_hitOriginGao ? m_hitOriginGao : null,
                ImpactLocation = m_impactOriginSocket ? m_impactOriginSocket.position : transform.position,
                Hit = m_hitDefinition,
                DamageMultiplier = GetDamageMultiplier(),
            };

            hpBehaviour.ApplyDamage(info);

            OnHit?.Invoke(info);
            return true;
        }

        public virtual float GetDamageMultiplier()
        {
            return m_damageMultiplier;
        }

#if UNITY_EDITOR
        private bool m_isDebugAttackRunning = false;

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void DebugAttack()
        {
            if (!m_isDebugAttackRunning)
            {
                StartCoroutine(DebugAttack_Coroutine());
            }
        }

        private IEnumerator DebugAttack_Coroutine()
        {
            m_isDebugAttackRunning = true;
            HitBegin();
            yield return new WaitForSeconds(0.2f);
            HitEnd();
            m_isDebugAttackRunning = false;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !m_collider || !m_collider.enabled) { return; }

            BoxCollider boxCollider = m_collider as BoxCollider;
            SphereCollider sphereCollider = m_collider as SphereCollider;

            Gizmos.color = Color.yellow;

            if (boxCollider)
            {
                Gizmos.DrawWireCube(transform.position + boxCollider.center, boxCollider.size);
            }
            else if (sphereCollider)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }
        }

#endif
    }
}