namespace NobunAtelier
{
    /// <summary>
    /// Empty interface to identify ability execution driver module definitions.
    /// </summary>
    public interface IAbilityExecutionDriverModuleDefinition {}

    /// <summary>
    /// Interface for ability execution drivers. Execution drivers are responsible for controlling the execution of an ability.
    /// </summary>
    public interface IAbilityExecutionDriver
    {
        void Initialize(in AbilityExecutionDriverContext context);
        void RequestExecution();
        void Reset();
        void Cancel();
    }

    /// <summary>
    /// Context passed to the execution driver when it is initialized.
    /// </summary>
    public readonly struct AbilityExecutionDriverContext
    {
        public IAbilityExecutionDriverCallbacks Callbacks { get; }

        public AbilityExecutionDriverContext(IAbilityExecutionDriverCallbacks callbacks)
        {
            Callbacks = callbacks;
        }
    }

    /// <summary>
    /// These callbacks are used to notify the ability instance that the execution has started, stopped, or completed.
    /// </summary>
    public interface IAbilityExecutionDriverCallbacks
    {
        void OnEffectStart();
        void OnEffectStop();
        void OnExecutionComplete();
    }
}