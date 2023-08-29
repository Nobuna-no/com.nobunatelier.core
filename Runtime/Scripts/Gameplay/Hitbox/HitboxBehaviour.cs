using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//[System.Serializable, System.Flags]
//public enum TeamPlaceholder
//{
//    Player = 1 << 1,
//    Flowers = 1 << 2,
//    Moles = 1 << 3,
//    Seeds = 1 << 4
//}

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class HitboxBehaviour : MonoBehaviour
    {
        public List<TeamDefinition> TargetTeam => m_targetTeam;

        // [SerializeField, Header("Hitbox")]
        // private TeamPlaceholder m_targetTeam;
        [Header("Hitbox")]
        [SerializeField, Tooltip("The owner of the hit. Can be use by the hit receiver to track the origin of the attack.")]
        private GameObject m_hitOrigin;

        [SerializeField]
        private List<TeamDefinition> m_targetTeam = new List<TeamDefinition>();

        [SerializeField]
        private Transform m_impactOriginSocket;

        [SerializeField]
        private HitDefinition m_hitDefinition;

        protected Collider OwnCollider => m_collider;
        private Collider m_collider;

        public UnityEvent OnHitboxEnabled;
        public UnityEvent OnHitboxDisabled;
        public HitEvent OnHit;

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
            if (hpBehaviour && !hpBehaviour.IsDead && m_targetTeam.Contains(hpBehaviour.Team))
            {
                HitInfo info = new HitInfo
                {
                    Origin = m_hitOrigin ? m_hitOrigin : this.gameObject,
                    ImpactLocation = m_impactOriginSocket ? m_impactOriginSocket.position : transform.position,
                    Hit = m_hitDefinition
                };
                hpBehaviour.ApplyDamage(info);

                OnHit?.Invoke(info);
                return true;
            }

            return false;
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