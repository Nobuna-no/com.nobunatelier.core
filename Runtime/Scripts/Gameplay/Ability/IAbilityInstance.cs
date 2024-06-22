using System;

public interface IAbilityInstance
{
    event Action OnAbilityStartCharge;

    event Action OnAbilityInitiated;

    event Action OnAbilityChainOpportunity;

    event Action OnAbilityCompleteExecution;

    AbilityDefinition Ability { get; }
    bool IsCharging { get; }

    /// <summary>
    /// </summary>
    /// <returns>Return true if the ability can be executed.</returns>
    bool CanExecute();

    /// <summary>
    /// update internal state in order to execute the ability.
    /// </summary>
    /// <returns></returns>
    void InitiateExecution();

    /// <summary>
    /// Start effect of the ability.
    /// </summary>
    void ExecuteEffect();

    /// <summary>
    /// Update ability, active between when internal state is InProgress
    /// (Between StartEffect and StopEffect).
    /// </summary>
    void UpdateEffect(float deltaTime);

    /// <summary>
    /// Stop effect of the ability.
    /// </summary>
    void StopEffect();

    /// <summary>
    /// Complete the execution of the ability and reset internal state.
    /// </summary>
    void TerminateExecution();

    void StartCharge();

    void ReleaseCharge();

    void CancelCharge();

    public enum ExecutionState
    {
        Ready,          // The idle and ready state.
        Starting,     // Before start affect is called.
        InProgress,     // After StartEffect is called until StopEffect.
        ChainOpportunity, // Window for chaining ability.
        Cooldown,       // CoolDown period after an ability when no followUp is available.
        Charging,
    }
}
