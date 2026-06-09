using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public partial class AbilityController : CharacterAbilityModuleBase
    {
        [Header("Ability Controller")]
        [SerializeField] private SkillDefinition m_DefaultSkill;

        [Header("Events")]
        [SerializeField] public UnityEvent OnAbilityStarted;
        [SerializeField] public UnityEvent OnAbilityStartCharge;
        [SerializeField] public UnityEvent OnRecoveryWindowOpen;
        [SerializeField] public UnityEvent OnAbilityCompleted;
        [SerializeField] public UnityEvent OnAbilityCancelled;

        [Header("Log")]
        [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

        public TeamModule Team => m_TeamModule;
        public GameplayTagModule TagModule => m_TagModule;
        public ContextualLogManager.LogPartition Log { get; private set; }

        public SkillDefinition CurrentSkill => m_Instance?.CurrentSkill;
        public ExecutionState CurrentState => m_Instance?.State ?? ExecutionState.Ready;
        public bool IsCharging => m_Instance?.IsCharging ?? false;
        public bool IsInRecovery => m_Instance?.IsInRecovery ?? false;
        public AbilityExecutionContext ExecutionContext => m_Instance?.ExecutionContext ?? default;

        private TeamModule m_TeamModule;
        private GameplayTagModule m_TagModule;
        private AbilityInstance m_Instance;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            ModuleOwner.TryGetAbilityModule(out m_TeamModule);
            Debug.Assert(m_TeamModule, $"{name}: Owner needs to be part of a team!", this);
            ModuleOwner.TryGetAbilityModule(out m_TagModule);
        }

        public bool TryExecute(SkillDefinition skill, AbilityExecutionContext? context = null)
        {
            if (!isActiveAndEnabled || skill == null)
                return false;

            EnsureInstance();
            return m_Instance.TryExecute(skill, context);
        }

        [Button]
        public void PlayDefaultAbility()
        {
            if (m_DefaultSkill == null)
            {
                Debug.LogWarning($"{name}: No default SkillDefinition set.", this);
                return;
            }

            TryExecute(m_DefaultSkill);
        }

        public void Cancel()
        {
            m_Instance?.Cancel();
            Log?.Record();
        }

        public bool StartCharge(SkillDefinition skill)
        {
            if (!isActiveAndEnabled || skill == null)
                return false;

            EnsureInstance();
            return m_Instance.StartCharge(skill);
        }

        public void ReleaseCharge()
        {
            m_Instance?.ReleaseCharge();
        }

        public void CancelCharge()
        {
            m_Instance?.CancelCharge();
        }

        protected override void OnAbilityUpdate(float deltaTime)
        {
            base.OnAbilityUpdate(deltaTime);
            m_Instance?.Update(deltaTime);
        }

        private void EnsureInstance()
        {
            if (m_Instance != null)
                return;

            m_Instance = new AbilityInstance(this);
            m_Instance.OnAbilityStarted += () => OnAbilityStarted?.Invoke();
            m_Instance.OnAbilityStartCharge += () => OnAbilityStartCharge?.Invoke();
            m_Instance.OnRecoveryWindowOpen += () => OnRecoveryWindowOpen?.Invoke();
            m_Instance.OnAbilityCompleted += () => OnAbilityCompleted?.Invoke();
            m_Instance.OnAbilityCancelled += () => OnAbilityCancelled?.Invoke();
        }

        private void OnEnable()
        {
            Log = ContextualLogManager.Register(this, m_LogSettings);
        }

        private void OnDisable()
        {
            ContextualLogManager.Unregister(Log);
            CleanupInstance();
        }

        private void OnDestroy()
        {
            CleanupInstance();
        }

        private void CleanupInstance()
        {
            if (m_Instance != null)
            {
                m_Instance.Dispose();
                m_Instance = null;
            }
        }
    }
}
