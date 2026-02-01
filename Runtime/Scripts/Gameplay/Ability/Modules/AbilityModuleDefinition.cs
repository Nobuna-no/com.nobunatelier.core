using NobunAtelier;
using UnityEngine;
using static AbilityModuleDefinition;

public abstract class AbilityModuleDefinition : DataDefinition
{
    public abstract IAbilityModuleInstance CreateInstance(AbilityController controller);

    public enum EffectTarget
    {
        Self,
        Target
    }
}

// If it grows, move to its own script.
public static class AbilityModuleHelper
{
    public static bool TryGetTarget(AbilityController controller, EffectTarget targetMode, out Transform m_target)
    {
        m_target = null;
        switch (targetMode)
        {
            case EffectTarget.Self:
                m_target = controller.ModuleOwner.Transform;
                break;

            case EffectTarget.Target:
                if (controller.Target == null)
                {
                    Debug.LogWarning($"Trying to position attack hit over target, but {controller.name} target has not been assigned...", controller);
                    m_target = controller.ModuleOwner.Transform;
                }
                else
                {
                    m_target = controller.Target;
                }
                break;

            default:
                break;
        }

        return m_target != null;
    }
}