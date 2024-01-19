using NaughtyAttributes;
using NobunAtelier;
using NobunAtelier.Gameplay;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public struct HitInfo
{
    public TeamModule OriginTeam;
    public GameObject OriginGao;
    public Vector3 ImpactLocation;
    public HitDefinition Hit;
}

[System.Serializable]
public class HitEvent : UnityEvent<HitInfo>
{ }

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(TeamModule))]
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

        public TeamDefinition Team => m_teamModule.Team;

        [Header("Definition")]
        [SerializeField, Required]
        private HealthDefinition m_definition;

        [SerializeField]
        private bool m_resetOnStart = false;

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

        public bool IsVulnerable
        {
            get => m_isVulnerable;
            set => m_isVulnerable = value;
        }

        private float m_currentInvulnerabilityDuration = 0f;
        private TeamModule m_teamModule;

        public delegate void OnHealthChangedDelegate(float currentHealth, float maxHealth);

        public event OnHealthChangedDelegate OnHealthChanged;

        private void Start()
        {
            if (m_resetOnStart)
            {
                Reset();
            }
        }

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
            m_teamModule = GetComponent<TeamModule>();

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

        public void ApplyDamage(HitInfo hitInfo, bool ignoreIframe = false)
        {
            if (m_isDead || (!m_isVulnerable && !ignoreIframe))
            {
                return;
            }

            if (hitInfo.Hit.DamageAmount < 0)
            {
                Debug.Log($"Trying to heal life using `ApplyDamage` on {this.gameObject}. Use `Heal` instead");
                return;
            }

            m_CurrentLifeValue = Mathf.Max(m_CurrentLifeValue - hitInfo.Hit.DamageAmount, 0);
            OnHealthChanged?.Invoke(m_CurrentLifeValue, m_definition.MaxValue);

            if (m_CurrentLifeValue <= 0)
            {
                m_isDead = true;
                OnDeath?.Invoke(hitInfo);
                StartCoroutine(PoolObjectDeactivateCoroutine());
            }
            else
            {
                OnHit?.Invoke(hitInfo);

                if (hitInfo.Hit.DamageAmount > 0)
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

        public void ApplyDamage(HitDefinition hit, Vector3 impactOrigin, TeamModule origin, bool ignoreIframe = false)
        {
            HitInfo info = new HitInfo { OriginTeam = origin, ImpactLocation = impactOrigin, Hit = hit };
            ApplyDamage(info, ignoreIframe);
        }

        public void ApplyDamage(HitDefinition hit, Vector3 impactOrigin, GameObject origin, bool ignoreIframe = false)
        {
            HitInfo info = new HitInfo { OriginGao = origin, ImpactLocation = impactOrigin, Hit = hit };
            ApplyDamage(info, ignoreIframe);
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