using NaughtyAttributes;
using System.Collections.Generic;
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
        [SerializeField] private UnityEvent OnAbilityStartCharge;
        [SerializeField] private UnityEvent OnAbilityStartExecution;
        [SerializeField] private UnityEvent OnAbilityChainOpportunity;
        [SerializeField] private UnityEvent OnAbilityCompleteExecution;

        [Header("Log")]
        [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

        public TeamModule Team => m_TeamModule;
        public ContextualLogManager.LogPartition Log { get; private set; }

        private Queue<System.Action> m_ActionsQueue = new Queue<System.Action>();

        private Processor m_AbilityProcessor;
        private Processor m_AbilityProcessorOverride;
        private TeamModule m_TeamModule;
        private bool m_CanExecuteNewAction = true;

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
            if (!isActiveAndEnabled || !m_CanExecuteNewAction)
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
            GetProcessorAndInitializeIfNeeded();

            QueueInitiateAbilityExecution();
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

            var activeProcessor = GetProcessorAndInitializeIfNeeded();

            if (activeProcessor != null)
            {
                activeProcessor.Terminate();
            }

            Log.Record($"{typeof(AnimComboModule).Name}: StopCombo.");

            if (m_AbilityProcessorOverride != null)
            {
                m_AbilityProcessorOverride = null;
            }
        }

        // Play ability but trying to use charge level settings.
        // If no charge level available, PlayAbility is called instead.
        public virtual void StartAbilityCharge()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.StartCharge();
        }

        public virtual void ReleaseAbilityCharge()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.ReleaseCharge();
        }

        /// <summary>
        /// In case a hit stun or player action cancel a charge:
        /// - Stop active abilityModuleEffect
        /// - Play any cancellation effect (useful for feedback?).
        /// </summary>
        public virtual void CancelAbilityCharge()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.CancelCharge();
        }

        public virtual void SetAbility(AbilityDefinition ability)
        {
            m_DefaultAbility = ability;
        }

        protected override void OnAbilityUpdate(float deltaTime)
        {
            base.OnAbilityUpdate(deltaTime);

            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.Update(deltaTime);

            if (!m_CanExecuteNewAction || m_ActionsQueue.Count == 0)
            {
                return;
            }

            Log.Record($"{this.name}{typeof(AnimComboModule).Name}: Dequeue next attack.");

            m_ActionsQueue.Dequeue().Invoke();
            m_CanExecuteNewAction = false;
        }

        /// <summary>
        /// Called when an ability has been initiated.
        /// </summary>
        protected virtual void OnAbilitySetup()
        {
        }

        /// <summary>
        /// Calls to handles the execution of an ability.
        /// After this function is called, you need to call the following in order:
        /// 1. StartAbilityEffect(); // Play the ability modules.
        /// 2. StopAbilityEffect(); // Stop the ability modules. ExecutionState -> ChainOpportunity.
        /// 3. CompleteAbilityExecution(); // Reset internal state. ExecutionState -> Ready.
        /// </summary>
        protected virtual void OnAbilityExecution()
        {
        }

        /// <summary>
        /// Play the ability modules effect.
        /// </summary>
        protected void StartAbilityEffect()
        {
            if (!isActiveAndEnabled/* || ActiveAbility == null*/)
            {
                return;
            }

            Log.Record($"{this.name}{typeof(AnimComboModule).Name}: AbilityEffectBegin.");

            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.PlayAbilityModules();
            // m_abilitiesModulesMap[m_activeAbility].PlayModules();
        }

        /// <summary>
        /// Stop the ability modules effect and change ExecutionState to ChainOpportunity.
        /// </summary>
        protected void StopAbilityEffect()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Log.Record($"{this.name}{typeof(AnimComboModule).Name}: AttackHitEnd.");

            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.StopAbilityModules();
            m_CanExecuteNewAction = true;
        }

        /// <summary>
        /// To be called after OnAbilityExecution to complete the ability life cycle.
        /// Reset execution state to Ready.
        /// </summary>
        protected void CompleteAbilityExecution()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();

            if (!isActiveAndEnabled)// || State == AbilityExecutionState.Charging)
            {
                Log.Record("Failed AttackEnd", ContextualLogManager.LogTypeFilter.Warning);
                return;
            }

            Log.Record();

            activeProcessor.Terminate();
            m_CanExecuteNewAction = true;
        }

        public void HandleEffectStartEvent()
        {
            Log.Record("Effect start event triggered");
            StartAbilityEffect();
        }

        public void HandleEffectStopEvent()
        {
            Log.Record("Effect stop event triggered");
            StopAbilityEffect();
        }

        public void HandleAnimationEndEvent()
        {
            Log.Record("Animation end event triggered");
            CompleteAbilityExecution();
        }

        internal void QueueInitiateAbilityExecution()
        {
            Log.Record();

            // Cache the combo action in a queue, later we can improve the queue with a TimingQueue.
            m_ActionsQueue.Enqueue(() =>
            {
                InitiateAbilityExecution();
            });
        }

        internal void EnqueueAbilityExecution()
        {
            OnAbilityExecution();
        }

        private Processor GetProcessorAndInitializeIfNeeded()
        {
            // If default processor not setup yet, init.
            if (m_AbilityProcessor == null)
            {
                m_AbilityProcessor = new Processor();
                m_AbilityProcessor.Initialize(this, m_DefaultAbility);
            }

            return m_AbilityProcessorOverride != null ? m_AbilityProcessorOverride : m_AbilityProcessor;
        }

        private void InitiateAbilityExecution()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();

            if (!activeProcessor.CanExecute())
            {
                return;
            }

            activeProcessor.Execute();
            m_CanExecuteNewAction = false;
        }

        private void OnEnable()
        {
            Log = ContextualLogManager.Register(this, m_LogSettings);
        }

        private void OnDisable()
        {
            ContextualLogManager.Unregister(Log);
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
