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
        [SerializeField] internal UnityEvent OnAbilityStartCharge;
        [SerializeField] internal UnityEvent OnAbilityStartExecution;
        [SerializeField] internal UnityEvent OnAbilityChainOpportunity;
        [SerializeField] internal UnityEvent OnAbilityCompleteExecution;

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
        /// Break the combo and reset. To use after taking a damage for instance.
        /// </summary>
        public virtual void StopAbility()
        {
            if (m_DefaultAbility == null)
            {
                Debug.LogWarning($"{this.name}: Trying to StopAbility, but no active {typeof(AbilityDefinition).Name} set." +
                    $"Call 'SetActiveAbility' or 'StopAbility({typeof(AbilityDefinition).Name} ability)' instead.", this);
                return;
            }

            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.StopAbility();

            Log.Record($"{typeof(AnimComboModule).Name}: StopCombo.");

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
        /// Called when an ability has been initiated.
        /// </summary>
        internal virtual void OnAbilitySetup()
        {
        }

        /// <summary>
        /// Calls to handles the execution of an ability.
        /// After this function is called, you need to call the following in order:
        /// 1. StartAbilityEffect(); // Play the ability modules.
        /// 2. StopAbilityEffect(); // Stop the ability modules. ExecutionState -> ChainOpportunity.
        /// 3. CompleteAbilityExecution(); // Reset internal state. ExecutionState -> Ready.
        /// </summary>
        internal virtual void OnAbilityExecution()
        {
        }

        /// <summary>
        /// Play the ability modules effect.
        /// </summary>
        internal void StartAbilityEffect()
        {
            if (!isActiveAndEnabled/* || ActiveAbility == null*/)
            {
                return;
            }

            Log.Record();

            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.PlayAbilityModules();
        }

        /// <summary>
        /// Stop the ability modules effect and change ExecutionState to ChainOpportunity.
        /// </summary>
        internal void StopAbilityEffect()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Log.Record();

            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.StopAbilityModules();
        }

        /// <summary>
        /// To be called after OnAbilityExecution to complete the ability life cycle.
        /// Reset execution state to Ready.
        /// </summary>
        internal void CompleteAbilityExecution()
        {
            if (!isActiveAndEnabled)// || State == AbilityExecutionState.Charging)
            {
                Log.Record("Failed AttackEnd", ContextualLogManager.LogTypeFilter.Warning);
                return;
            }

            Log.Record();

            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.CompleteAbilityExecution();
        }

        public void HandleEffectStartEvent()
        {
            Log.Record("Start ability effect event triggered");
            StartAbilityEffect();
        }

        public void HandleEffectStopEvent()
        {
            Log.Record("Stop ability effect event triggered");
            StopAbilityEffect();
        }

        public void HandleAnimationEndEvent()
        {
            Log.Record("Complete ability execution event triggered");
            CompleteAbilityExecution();
        }

        internal void QueueInitiateAbilityExecution()
        {
            var runtime = GetRuntimeAndInitializeIfNeeded();
            runtime.QueueInitiateAbilityExecution();
        }

        internal void EnqueueAbilityExecution()
        {
            OnAbilityExecution();
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

        //public enum AbilityExecutionState
        //{
        //    Ready,          // The idle and ready state.
        //    InProgress,     // Ongoing ability execution.
        //    ChainOpportunity, // Window for chaining ability.
        //    Cooldown,       // CoolDown period after an ability when no followUp is available.
        //    Charging,
        //}
    }
}
