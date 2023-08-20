using NaughtyAttributes;
using NobunAtelier.Gameplay;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public struct HitInfo
{
    public GameObject Origin;
    public Vector3 ImpactLocation;
    public HitDefinition Hit;
}

[System.Serializable]
public class HitEvent : UnityEvent<HitInfo>
{ }

namespace NobunAtelier.Gameplay
{
    public class HealthBehaviour : CharacterAbilityModuleBase
    {
        public static HitDefinition KillHit
        {
            get
            {
                if (s_killHit == null)
                {
                    s_killHit = HitDefinition.Create(999);
                }

                return s_killHit;
            }
        }

        private static HitDefinition s_killHit = null;

        public TeamDefinition Team => m_team;
        public bool IsVulnerable => IsVulnerable;

        [Header("Definition")]
        [SerializeField]
        private TeamDefinition m_team;

        [SerializeField, Required]
        private HealthDefinition m_definition;

        [SerializeField, Header("Death")]
        private GameObject m_objectToMakeDisappear;

        [Foldout("Events")]
        public HitEvent OnHit;

        [Foldout("Events")]
        public UnityEvent OnInvulnerabilityBegin;

        [Foldout("Events")]
        public UnityEvent OnInvulnerabilityEnd;

        [Foldout("Events")]
        public UnityEvent OnHeal;

        [Foldout("Events")]
        public HitEvent OnDeath;

        [Foldout("Events")]
        public UnityEvent OnBurial;

        [Foldout("Events")]
        public UnityEvent OnResurrection;

        [Foldout("Events")]
        public UnityEvent OnDestroy;

        [Foldout("Events")]
        public UnityEvent OnDisappearing;

        [Foldout("Events")]
        public UnityEvent OnReset;

        [SerializeField, Foldout("Debug"), ReadOnly]
        private float m_CurrentLifeValue = 0;

        [SerializeField, Foldout("Debug")]
        private HitDefinition m_debugHitDefinition;

        public bool IsDead => m_isDead;
        private bool m_isDead = false;

        private bool m_isVulnerable = true;

        private float m_currentInvulnerabilityDuration = 0f;

        public delegate void OnHealthChangedDelegate(float currentHealth, float maxHealth);

        public event OnHealthChangedDelegate OnHealthChanged;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Reset();
            Debug.Assert(m_definition != null);
        }

        public override void Reset()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            base.Reset();

            OnReset?.Invoke();

            if (m_isDead)
            {
                Resurrect();
            }
            else
            {
                m_CurrentLifeValue = m_definition.InitialValue;
                OnHealthChanged?.Invoke(m_CurrentLifeValue, m_definition.MaxValue);
            }
        }

        public void Heal(float amount)
        {
            if (m_isDead)
            {
                Debug.LogWarning($"Trying to heal a dead character {this.ModuleOwner.name}. Use Resurrect instead.");
                return;
            }

            if (amount < 0)
            {
                Debug.Log($"Trying to inflict damage using `Heal` on {this.gameObject}. Use `ApplyDamage` instead");
                return;
            }

            m_CurrentLifeValue = Mathf.Min(m_CurrentLifeValue + amount, m_definition.MaxValue);

            OnHealthChanged?.Invoke(m_CurrentLifeValue, m_definition.MaxValue);
            OnHeal?.Invoke();
        }

        public void ApplyDamage(HitDefinition hit, Vector3 impactOrigin, GameObject origin, bool ignoreIframe = false)
        {
            if (m_isDead || (!m_isVulnerable && !ignoreIframe))
            {
                return;
            }

            if (hit.DamageAmount < 0)
            {
                Debug.Log($"Trying to heal life using `ApplyDamage` on {this.gameObject}. Use `Heal` instead");
                return;
            }

            m_CurrentLifeValue = Mathf.Max(m_CurrentLifeValue - hit.DamageAmount, 0);
            OnHealthChanged?.Invoke(m_CurrentLifeValue, m_definition.MaxValue);

            HitInfo info = new HitInfo { Origin = origin, ImpactLocation = impactOrigin, Hit = hit };

            if (m_CurrentLifeValue <= 0)
            {
                m_isDead = true;
                OnDeath?.Invoke(info);
                StartCoroutine(PoolObjectDeactivateCoroutine());
            }
            else
            {
                OnHit?.Invoke(info);

                if (hit.DamageAmount > 0)
                {
                    m_currentInvulnerabilityDuration = m_definition.InvulnerabilityDuration;
                    if (m_isVulnerable)
                    {
                        m_isVulnerable = false;
                        StartCoroutine(InvulnerabilityCoroutine());
                    }
                }
            }
        }

        public void Resurrect(float lifeAmount = -1)
        {
            if (!m_isDead)
            {
                return;
            }

            m_isDead = false;
            m_CurrentLifeValue = lifeAmount > 0 ? lifeAmount : m_definition.InitialValue;
            OnHealthChanged?.Invoke(m_CurrentLifeValue, m_definition.MaxValue);
            OnResurrection?.Invoke();

            if (m_objectToMakeDisappear)
            {
                m_objectToMakeDisappear.SetActive(true);
            }
        }

        [Button("Kill", EButtonEnableMode.Playmode)]
        public void Kill()
        {
            ApplyDamage(KillHit, transform.position, this.gameObject, true);
        }

        public void Disappear()
        {
            if (!m_isDead)
            {
                return;
            }

            if (m_objectToMakeDisappear)
            {
                m_objectToMakeDisappear.SetActive(false);
            }
            OnDisappearing?.Invoke();
        }

        [Button("Resurrect", EButtonEnableMode.Playmode)]
        private void Resurrect_Debug()
        {
            Resurrect();
        }

        [Button("Apply Debug Hit Damage", EButtonEnableMode.Playmode)]
        private void ApplyDebugDamage_Debug()
        {
            if (m_debugHitDefinition)
            {
                ApplyDamage(m_debugHitDefinition, transform.position, this.gameObject);
            }
        }

        private IEnumerator PoolObjectDeactivateCoroutine()
        {
            float value = Random.Range(m_definition.BurialDelay.x, m_definition.BurialDelay.y);
            yield return new WaitForSecondsRealtime(value);

            OnBurial?.Invoke();

            if (m_definition.Burial == HealthDefinition.BurialType.None)
            {
                yield break;
            }

            switch (m_definition.Burial)
            {
                case HealthDefinition.BurialType.Disappear:
                    Disappear();
                    break;

                case HealthDefinition.BurialType.Destroy:
                    OnDestroy?.Invoke();
                    Destroy(this);
                    break;

                case HealthDefinition.BurialType.Resurect:
                    Resurrect();
                    break;
            }
        }

        private IEnumerator InvulnerabilityCoroutine()
        {
            OnInvulnerabilityBegin?.Invoke();

            while (m_currentInvulnerabilityDuration > 0)
            {
                yield return new WaitForFixedUpdate();
                m_currentInvulnerabilityDuration -= Time.fixedDeltaTime;
            }

            OnInvulnerabilityEnd?.Invoke();
            m_isVulnerable = true;
        }
    }
}