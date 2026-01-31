using NobunAtelier;
using System.Collections.Generic;

/// <summary>
/// Helper class to control ability modules.
/// </summary>
public class AbilityModuleController
{
    public Dictionary<AbilityModuleDefinition, IAbilityModuleInstance> m_modulesMap;
    private AbilityController m_controller;

    public AbilityModuleController(AbilityController controller)
    {
        m_modulesMap = new Dictionary<AbilityModuleDefinition, IAbilityModuleInstance>();
        m_controller = controller;
    }

    public void Add(AbilityModuleDefinition module)
    {
        if (m_modulesMap.ContainsKey(module))
        {
            return;
        }

        m_modulesMap.Add(module, module.CreateInstance(m_controller));
    }

    public void Add(IReadOnlyCollection<AbilityModuleDefinition> modules)
    {
        foreach (var module in modules)
        {
            Add(module);
        }
    }

    public void Remove(AbilityModuleDefinition module)
    {
        if (!m_modulesMap.ContainsKey(module))
        {
            return;
        }

        m_modulesMap.Remove(module);
    }

    public void Remove(IReadOnlyCollection<AbilityModuleDefinition> modules)
    {
        foreach (var module in modules)
        {
            Remove(module);
        }
    }

    public void ExecuteModule(AbilityModuleDefinition module)
    {
        if (m_modulesMap.TryGetValue(module, out var instance))
        {
            instance.ExecuteEffect();
        }
    }

    public void ExecuteModules(IReadOnlyCollection<AbilityModuleDefinition> modules)
    {
        foreach (var module in modules)
        {
            if (m_modulesMap.TryGetValue(module, out var instance))
            {
                instance.ExecuteEffect();
            }
        }
    }

    public void StopModule(AbilityModuleDefinition module, bool includeProcessors)
    {
        if (m_modulesMap.TryGetValue(module, out var instance))
        {
            if (!includeProcessors && instance is IModularAbilityProcessor)
            {
                return;
            }

            instance.Stop();
        }
    }

    public void StopModules(IReadOnlyCollection<AbilityModuleDefinition> modules, bool includeProcessors)
    {
        foreach (var module in modules)
        {
            if (m_modulesMap.TryGetValue(module, out var instance))
            {
                if (!includeProcessors && instance is IModularAbilityProcessor)
                {
                    continue;
                }

                instance.Stop();
            }
        }
    }

    private void StopModules()
    {
        foreach (var module in m_modulesMap.Values)
        {
            module.Stop();
        }
    }

    public void UpdateModule(float deltaTime, AbilityModuleDefinition module)
    {
        if (m_modulesMap.TryGetValue(module, out var instance))
        {
            if (!instance.RunUpdate)
            {
                return;
            }

            instance.Update(deltaTime);
        }
    }

    public void UpdateModules(float deltaTime, IReadOnlyCollection<AbilityModuleDefinition> modules)
    {
        foreach (var module in modules)
        {
            if (m_modulesMap.TryGetValue(module, out var instance))
            {
                if (!instance.RunUpdate)
                {
                    continue;
                }

                instance.Update(deltaTime);
            }
        }
    }

    public void InitiateModulesExecution(IReadOnlyCollection<AbilityModuleDefinition> modules)
    {
        StopModules();

        foreach (var module in modules)
        {
            if (m_modulesMap.TryGetValue(module, out var instance))
            {
                instance.InitiateExecution();
            }
        }
    }
}
