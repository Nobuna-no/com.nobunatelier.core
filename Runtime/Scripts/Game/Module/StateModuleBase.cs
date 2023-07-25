using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class StateModuleBase : MonoBehaviour
    {
        private IState<StateDefinition> m_moduleOwner;
        public IState<StateDefinition> ModuleOwner => m_moduleOwner;

        public virtual void Init<T>(StateComponent<T> owner) where T : StateDefinition
        {
            m_moduleOwner = owner as IState<StateDefinition>;
            Debug.LogWarning($"ModuleOwner is: {m_moduleOwner}");
        }

        public virtual void Init<T>(StateComponent<StateDefinition> owner) where T : StateDefinition
        {
            m_moduleOwner = owner;
            Debug.LogWarning($"ModuleOwner is: {m_moduleOwner}");
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }
}
