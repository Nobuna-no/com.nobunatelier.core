using System.Threading;

namespace NobunAtelier
{
    /// <summary>
    /// Phases of ability execution. Drivers signal phase transitions via
    /// <see cref="IAbilityActionDriverCallbacks.OnPhaseTransition"/>.
    /// AbilityInstance owns the state machine and transitions accordingly.
    /// </summary>
    public enum AbilityPhase
    {
        /// <summary>Startup complete, effects begin. Starting -> InProgress.</summary>
        Active,

        /// <summary>Active phase done, recovery window opens. InProgress -> Recovery.</summary>
        Recovery,

        /// <summary>Execution fully complete, cleanup. Recovery -> Ready.</summary>
        Complete
    }

    /// <summary>
    /// Timing source that fires <see cref="GameplayEventDefinition"/>s and signals
    /// <see cref="AbilityPhase"/> transitions during ability execution.
    /// </summary>
    public interface IAbilityActionDriver
    {
        /// <summary>
        /// Returns all content GameplayEvents this driver can fire.
        /// </summary>
        GameplayEventDefinition[] GetAvailableEvents();

        /// <summary>
        /// Initialize the driver with context (callbacks, cancellation, controller reference).
        /// </summary>
        void Initialize(in AbilityActionDriverContext context);

        /// <summary>
        /// Start the driver's execution sequence.
        /// </summary>
        void RequestExecution();

        /// <summary>
        /// Normal cleanup after execution completes.
        /// </summary>
        void Reset();

        /// <summary>
        /// Forced cancellation during execution.
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// Callback interface for drivers to fire content events and signal phase transitions.
    /// </summary>
    public interface IAbilityActionDriverCallbacks
    {
        /// <summary>
        /// Fire a content <see cref="GameplayEventDefinition"/> (Hit1, TrailStart, etc.).
        /// Dispatches to matching <see cref="GameplayEventGroup"/> entries.
        /// </summary>
        void FireEvent(GameplayEventDefinition gameplayEvent);

        /// <summary>
        /// Signal an ability phase transition. AbilityInstance handles state machine
        /// transitions and fires phase-bound effects from AbilityAction.
        /// </summary>
        void OnPhaseTransition(AbilityPhase phase);
    }

    /// <summary>
    /// Context passed to <see cref="IAbilityActionDriver.Initialize"/>.
    /// </summary>
    public readonly struct AbilityActionDriverContext
    {
        public IAbilityActionDriverCallbacks Callbacks { get; }
        public CancellationToken Token { get; }
        public AbilityController Controller { get; }

        public AbilityActionDriverContext(
            IAbilityActionDriverCallbacks callbacks,
            CancellationToken token,
            AbilityController controller)
        {
            Callbacks = callbacks;
            Token = token;
            Controller = controller;
        }
    }
}
