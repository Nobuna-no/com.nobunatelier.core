using NobunAtelier;

// We need this base class for serialization.
public interface IAbilityModuleInstance
{
    public abstract bool RunUpdate { get; }
    public abstract void InitiateExecution();
    // public virtual void OnAbilityExecution() { }
    public abstract void ExecuteEffect();
    public abstract void Update(float deltaTime);
    public abstract void Stop();
}

public interface IModularAbilityProcessor
{
    public void RequestExecution();
}

/// <summary>
/// Runtime instance of a behavior composing an ability, each module defines its own behavior such as damage, animation, audio, ...
/// </summary>
/// <typeparam name="T">AbilityModuleDefinition</typeparam>
public abstract class AbilityModuleInstance<T> : DataInstance<T>, IAbilityModuleInstance
    where T : AbilityModuleDefinition
{
    public AbilityController Controller => m_controller;
    private AbilityController m_controller;
    public abstract bool RunUpdate { get; }

    public AbilityModuleInstance(AbilityController controller, T data)
        : base(data)
    {
        m_controller = controller;
    }

    public abstract void InitiateExecution();
    public abstract void ExecuteEffect();
    public abstract void Stop();
    public virtual void Update(float deltaTime) { }
}

/// Forgot why I did that...
//public abstract class AbilityModuleExecutionRequesterInstance<T> : AbilityModuleInstance<T>
//    where T : AbilityModuleDefinition
//{
//    public AbilityModuleExecutionRequesterInstance(AbilityController controller, T data)
//        : base(controller, data)
//    { }

//    public override void InitiateExecution()
//    {
//        //
//    }
//}