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
        [SerializeField] public UnityEvent OnAbilityStartCharge;
        [SerializeField] public UnityEvent OnAbilityStartExecution;
        [SerializeField] public UnityEvent OnAbilityChainOpportunity;
        [SerializeField] public UnityEvent OnAbilityCompleteExecution;

        [Header("Input Buffer")]
        [SerializeField] private float m_InputBufferDuration = 0.15f;
        [SerializeField] private bool m_InputBufferUseUnscaledTime = false;

        [Header("Log")]
        [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

        public TeamModule Team => m_TeamModule;
        public ContextualLogManager.LogPartition Log { get; private set; }
        internal float InputBufferDuration => m_InputBufferDuration;
        internal bool InputBufferUseUnscaledTime => m_InputBufferUseUnscaledTime;

        private TeamModule m_TeamModule;
        private AbilityRuntime m_Runtime;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            // m_chainIndex = 0;
            ModuleOwner.TryGetAbilityModule(out m_TeamModule);
            Debug.Assert(m_TeamModule, $"{this.name}: Owner need to be part of a team!", this);
        }

        [Button]
        public virtual void PlayAbility()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (m_DefaultAbility == null)
            {
                Debug.LogWarning($"{this.name}: Trying to PlayAbility, but no active {typeof(AbilityDefinition).Name} set." +
                    $"Call 'SetActiveAbility' or 'PlayAbility({typeof(AbilityDefinition).Name} ability)' instead.", this);
                return;
            }

            // ensure we have a default processor setup.
            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.QueueInitiateAbilityExecution();
        }

        /// <summary>
        /// Cancel the current ability execution and reset the ability instance.
        /// </summary>
        public virtual void StopAbility()
        {
            CancelAbility();
        }

        /// <summary>
        /// Hard cancel the current ability execution (death/parry/cancel).
        /// </summary>
        public virtual void CancelAbility()
        {
            if (m_DefaultAbility == null)
            {
                Debug.LogWarning($"{this.name}: Trying to StopAbility, but no active {typeof(AbilityDefinition).Name} set." +
                    $"Call 'SetActiveAbility' or 'StopAbility({typeof(AbilityDefinition).Name} ability)' instead.", this);
                return;
            }

            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.CancelAbility();

            Log.Record();
        }

        // Play ability but trying to use charge level settings.
        // If no charge level available, PlayAbility is called instead.
        public virtual void StartAbilityCharge()
        {
            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.StartCharge();
        }

        public virtual void ReleaseAbilityCharge()
        {
            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.ReleaseCharge();
        }

        /// <summary>
        /// In case a hit stun or player action cancel a charge:
        /// - Stop active abilityModuleEffect
        /// - Play any cancellation effect (useful for feedback?).
        /// </summary>
        public virtual void CancelAbilityCharge()
        {
            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.CancelCharge();
        }

        public virtual void SetAbility(AbilityDefinition ability)
        {
            m_DefaultAbility = ability;
            m_Runtime?.SetAbility(ability);
        }

        protected override void OnAbilityUpdate(float deltaTime)
        {
            base.OnAbilityUpdate(deltaTime);

            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.Update(deltaTime);
        }

        /// <summary>
        internal void QueueInitiateAbilityExecution()
        {
            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.QueueInitiateAbilityExecution();
        }

        private AbilityRuntime GetRuntimeAndInitializeIfNeeded()
        {
            if (m_Runtime == null)
            {
                m_Runtime = new AbilityRuntime();
            }

            m_Runtime.Initialize(this, m_DefaultAbility);
            return m_Runtime;
        }

        private void OnEnable()
        {
            Log = ContextualLogManager.Register(this, m_LogSettings);
        }

        private void OnDisable()
        {
            ContextualLogManager.Unregister(Log);
            CleanupRuntime();
        }

        private void OnDestroy()
        {
            CleanupRuntime();
        }

        private void CleanupRuntime()
        {
            if (m_Runtime != null)
            {
                m_Runtime.Dispose();
                m_Runtime = null;
            }
        }
    }
}
