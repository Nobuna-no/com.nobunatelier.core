using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
//    public abstract partial class AbilityController
//    {
//        private class ModuleRuntimeObjects
//        {
//            public Dictionary<BattleAbilityModuleDefinition, BattleAbilityModuleDefinition.IRuntimeObject> modules;
//            private bool m_hasModulesBeenStopped = false;

//            public ModuleRuntimeObjects(BattleAbilityDefinition ability)
//            {
//                modules = new Dictionary<BattleAbilityModuleDefinition,
//                    BattleAbilityModuleDefinition.IRuntimeObject>(ability.Modules.Count);

//                for (int i = 0; i < ability.Modules.Count; ++i)
//                {
//                    if (modules.ContainsKey(ability.Modules[i]))
//                    {
//                        // We don't want to have duplicate modules...
//                        continue;
//                    }

//                    modules.Add(ability.Modules[i], ability.Modules[i].CreateRuntimeObject());
//                }
//            }

//            public ModuleRuntimeObjects(IReadOnlyCollection<BattleAbilityModuleDefinition> OnLevelReachedEffects)
//            {
//                modules = new Dictionary<BattleAbilityModuleDefinition,
//                    BattleAbilityModuleDefinition.IRuntimeObject>(OnLevelReachedEffects.Count);

//                foreach (var module in OnLevelReachedEffects)
//                {
//                    if (modules.ContainsKey(module))
//                    {
//                        // We don't want to have duplicate modules...
//                        continue;
//                    }
//                    modules.Add(module, module.CreateRuntimeObject());
//                }
//            }

//            ~ModuleRuntimeObjects()
//            {
//                if (!m_hasModulesBeenStopped)
//                {
//                    StopModules();
//                }
//            }

//            public void Initialize(BattleAbilityController controller)
//            {
//                foreach (var module in modules.Values)
//                {
//                    module.Initialize(controller);
//                }
//            }

//            public void PlayModules()
//            {
//                foreach (var module in modules.Values)
//                {
//                    module.PlayEffect();
//                }
//                m_hasModulesBeenStopped = false;
//            }

//// #if UNITY_EDITOR

//            /// <summary>
//            /// Force reset all object for sake of having the most up to date version
//            /// of the data. Ensure we can edit definition in editor and get expected behaviour.
//            /// </summary>
//            /// <param name="ability"></param>
//            public void Refresh(BattleAbilityDefinition ability)
//            {
//                StopModules();
//                modules.Clear();

//                for (int i = 0; i < ability.Modules.Count; ++i)
//                {
//                    if (modules.ContainsKey(ability.Modules[i]))
//                    {
//                        // We don't want to have duplicate modules...
//                        continue;
//                    }

//                    modules.Add(ability.Modules[i], ability.Modules[i].CreateRuntimeObject());
//                }
//            }

//// #endif

//            public void StopModules()
//            {
//                foreach (var module in modules.Values)
//                {
//                    module.StopEffect();
//                }
//                m_hasModulesBeenStopped = true;
//            }

//            public void UpdateModules(float deltaTime)
//            {
//                foreach (var module in modules.Values)
//                {
//                    if (!module.RunUpdate)
//                    {
//                        return;
//                    }

//                    module.Update(deltaTime);
//                }
//            }
//        }

//        // This object represent a container that will be use as a key to.
//        public class ModuleCollection
//        {
//            public IReadOnlyList<BattleAbilityModuleDefinition> Effects { get; private set; }

//            public ModuleCollection(IReadOnlyList<BattleAbilityModuleDefinition> effects)
//            {
//                Effects = effects;
//            }
//        }
//    }
}