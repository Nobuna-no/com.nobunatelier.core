using System.Collections.Generic;

namespace NobunAtelier
{
    /// <summary>
    /// Helper class to control ability modules. 
    /// </summary>
    public class AbilityModuleRegistry
    {
        public Dictionary<AbilityModuleDefinition, IAbilityModuleInstance> m_ModulesMap;
        private AbilityController m_Controller;

        public AbilityModuleRegistry(AbilityController controller)
        {
            m_ModulesMap = new Dictionary<AbilityModuleDefinition, IAbilityModuleInstance>();
            m_Controller = controller;
        }

        public void Add(AbilityModuleDefinition module)
        {
            if (m_ModulesMap.ContainsKey(module))
            {
                return;
            }

            m_ModulesMap.Add(module, module.CreateInstance(m_Controller));
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
            if (!m_ModulesMap.ContainsKey(module))
            {
                return;
            }

            m_ModulesMap.Remove(module);
        }

        public void Remove(IReadOnlyCollection<AbilityModuleDefinition> modules)
        {
            foreach (var module in modules)
            {
                Remove(module);
            }
        }

        public void InitiateModulesExecution(IReadOnlyCollection<AbilityModuleDefinition> modules)
        {
            StopModules();

            foreach (var module in modules)
            {
                if (m_ModulesMap.TryGetValue(module, out var instance))
                {
                    instance.InitiateExecution();
                }
            }
        }

        public void ExecuteModule(AbilityModuleDefinition module)
        {
            if (m_ModulesMap.TryGetValue(module, out var instance))
            {
                instance.ExecuteEffect();
            }
        }

        public void ExecuteModules(IReadOnlyCollection<AbilityModuleDefinition> modules)
        {
            foreach (var module in modules)
            {
                if (m_ModulesMap.TryGetValue(module, out var instance))
                {
                    instance.ExecuteEffect();
                }
            }
        }

        public void StopModule(AbilityModuleDefinition module)
        {
            if (m_ModulesMap.TryGetValue(module, out var instance))
            {
                instance.Stop();
            }
        }

        public void StopModules(IReadOnlyCollection<AbilityModuleDefinition> modules)
        {
            foreach (var module in modules)
            {
                if (m_ModulesMap.TryGetValue(module, out var instance))
                {
                    instance.Stop();
                }
            }
        }

        private void StopModules()
        {
            foreach (var module in m_ModulesMap.Values)
            {
                module.Stop();
            }
        }

        public void UpdateModule(float deltaTime, AbilityModuleDefinition module)
        {
            if (m_ModulesMap.TryGetValue(module, out var instance))
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
                if (m_ModulesMap.TryGetValue(module, out var instance))
                {
                    if (!instance.RunUpdate)
                    {
                        continue;
                    }

                    instance.Update(deltaTime);
                }
            }
        }
    }
}