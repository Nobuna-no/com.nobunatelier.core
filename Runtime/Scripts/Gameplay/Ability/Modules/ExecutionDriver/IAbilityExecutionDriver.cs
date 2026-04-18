using System.Threading;

namespace NobunAtelier
{
    /// <summary>
    /// Empty interface to identify ability execution driver module definitions.
    /// </summary>
    public interface IAbilityExecutionDriverModuleDefinition { }

    /// <summary>
    /// Interface for ability execution drivers that control the timing lifecycle of an ability command.
    /// </summary>
    public interface IAbilityExecutionDriver
    {
        void Initialize(in AbilityExecutionDriverContext context);
        void RequestExecution();
        void Reset();
        void Cancel();
    }

    /// <summary>
    /// Context passed to the execution driver on initialization.
    /// </summary>
    public readonly struct AbilityExecutionDriverContext
    {
        public IAbilityExecutionDriverCallbacks Callbacks { get; }
        public CancellationToken Token { get; }

        public AbilityExecutionDriverContext(IAbilityExecutionDriverCallbacks callbacks, CancellationToken token = default)
        {
            Callbacks = callbacks;
            Token = token;
        }
    }

    /// <summary>
    /// Callbacks used by execution drivers to notify the ability instance of lifecycle transitions.
    /// </summary>
    public interface IAbilityExecutionDriverCallbacks
    {
        void OnEffectStart();
        void OnEffectStop();
        void OnExecutionComplete();
    }
}
