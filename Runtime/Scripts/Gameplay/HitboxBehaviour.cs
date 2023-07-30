using NaughtyAttributes;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable, System.Flags]
public enum TeamPlaceholder
{
    Player = 1 << 1,
    Flowers = 1 << 2,
    Moles = 1 << 3,
    Seeds = 1 << 4
}

public interface ITeamTaggable
{
    TeamPlaceholder Team { get; }
}

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class HitboxBehaviour : MonoBehaviour
    {
        [SerializeField, Header("Hitbox")]
        private TeamPlaceholder m_targetTeam;

        [SerializeField]
        private Transform m_impactOriginSocket;

        [SerializeField]
        private HitDefinition m_hitDefinition;

        protected Collider OwnCollider => m_collider;
        private Collider m_collider;

        public UnityEvent OnHit;

        public void SetTargetTeam(TeamPlaceholder targetTeam)
        {
            m_targetTeam = targetTeam;
        }

        public void SetHitDefinition(HitDefinition hit)
        {
            m_hitDefinition = hit;
        }

        public virtual void HitBegin()
        {
            m_collider.enabled = true;
        }

        public virtual void HitEnd()
        {
            m_collider.enabled = false;
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

        private void Awake()
        {
            // Debug.Assert(m_hitDefinition != null, $"{this} from {this.gameObject} doesn't have a HitDefinition.");
            m_collider = GetComponent<Collider>();
            m_collider.isTrigger = true;
            HitEnd();
        }

        protected bool TryDamageApply(Collider other)
        {
            if(other == null || m_hitDefinition == null)
            {
                return false;
            }

            var hpComp = other.GetComponent<HealthBehaviour>();
            if (hpComp && (hpComp.Team & m_targetTeam) != 0)
            {
                hpComp.ApplyDamage(m_hitDefinition, m_impactOriginSocket ? m_impactOriginSocket.position : transform.position, this.gameObject);
                OnHit?.Invoke();
                return true;
            }

            return false;
        }


#if UNITY_EDITOR
        private bool m_isDebugAttackRunning = false;
        [Button(enabledMode:EButtonEnableMode.Playmode)]
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

            Gizmos.color = (m_targetTeam & TeamPlaceholder.Player) != 0 ? Color.red : Color.cyan;

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