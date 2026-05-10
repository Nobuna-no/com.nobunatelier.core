using System.Threading;

namespace NobunAtelier
{
    /// <summary>
    /// Timing source that fires <see cref="GameplayEventDefinition"/>s during ability execution.
    /// Implementations: AnimDrivenAbilityAction (animation), AwaitableDrivenAbilityAction (timer).
    /// </summary>
    public interface IAbilityActionDriver
    {
        /// <summary>
        /// Returns all GameplayEvents this driver can fire.
        /// Used by AbilityAction for editor validation and binding setup.
        /// </summary>
        GameplayEventDefinition[] GetAvailableEvents();

        /// <summary>
        /// Initialize the driver with context (callbacks, cancellation, controller reference).
        /// Called once per action activation before <see cref="RequestExecution"/>.
        /// </summary>
        void Initialize(in AbilityActionDriverContext context);

        /// <summary>
        /// Start the driver's execution sequence.
        /// The driver fires events via <see cref="IAbilityActionDriverCallbacks.FireEvent"/> as its timing source dictates.
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
    /// Callback interface for drivers to fire events to the action execution layer.
    /// Single event channel replaces V7's three fixed callbacks (OnEffectStart/Stop/Complete).
    /// </summary>
    public interface IAbilityActionDriverCallbacks
    {
        /// <summary>
        /// Fire a <see cref="GameplayEventDefinition"/>. ActionExecution dispatches this to matching <see cref="EventBinding"/>s.
        /// Both structural events (EffectStart, EffectStop, ExecutionComplete) and
        /// content events (Hit1, TrailStart, etc.) flow through this single channel.
        /// </summary>
        void FireEvent(GameplayEventDefinition gameplayEvent);
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
