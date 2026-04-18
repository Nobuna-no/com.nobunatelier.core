using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public partial class AbilityController : CharacterAbilityModuleBase
    {
        [Header("Ability Controller")]
        [FormerlySerializedAs("m_defaultAbility")]
        [SerializeField] private AbilityDefinition m_DefaultAbility;

        [Header("Events")]
        [FormerlySerializedAs("OnAbilityStartExecution")]
        [SerializeField] public UnityEvent OnAbilityStarted;
        [SerializeField] public UnityEvent OnAbilityStartCharge;
        [FormerlySerializedAs("OnAbilityChainOpportunity")]
        [SerializeField] public UnityEvent OnRecoveryWindowOpen;
        [FormerlySerializedAs("OnAbilityCompleteExecution")]
        [SerializeField] public UnityEvent OnAbilityCompleted;
        [SerializeField] public UnityEvent OnAbilityCancelled;

        [Header("Log")]
        [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

        public TeamModule Team => m_TeamModule;
        public ContextualLogManager.LogPartition Log { get; private set; }

        public AbilityDefinition CurrentAbility => m_Instance?.CurrentAbility;
        public ExecutionState CurrentState => m_Instance?.State ?? ExecutionState.Ready;
        public bool IsCharging => m_Instance?.IsCharging ?? false;
        public bool IsInRecovery => m_Instance?.IsInRecovery ?? false;
        public AbilityExecutionContext ExecutionContext => m_Instance?.ExecutionContext ?? default;

        private TeamModule m_TeamModule;
        private AbilityInstance m_Instance;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            ModuleOwner.TryGetAbilityModule(out m_TeamModule);
            Debug.Assert(m_TeamModule, $"{name}: Owner needs to be part of a team!", this);
        }

        public bool TryExecute(AbilityDefinition ability, AbilityExecutionContext? context = null)
        {
            if (!isActiveAndEnabled || ability == null)
            {
                return false;
            }

            EnsureInstance();
            return m_Instance.TryExecute(ability, context);
        }

        [Button]
        public void PlayDefaultAbility()
        {
            if (m_DefaultAbility == null)
            {
                Debug.LogWarning($"{name}: No default AbilityDefinition set.", this);
                return;
            }

            TryExecute(m_DefaultAbility);
        }

        public void Cancel()
        {
            m_Instance?.Cancel();
            Log?.Record();
        }

        public bool StartCharge(AbilityDefinition ability)
        {
            if (!isActiveAndEnabled || ability == null)
            {
                return false;
            }

            EnsureInstance();
            return m_Instance.StartCharge(ability);
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
            {
                return;
            }

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
