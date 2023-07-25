using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class StateModuleBase : MonoBehaviour
    {
        private StateComponent m_moduleOwner;
        public StateComponent ModuleOwner => m_moduleOwner;

        public virtual void Init(StateComponent moduleOwner)
        {
            m_moduleOwner = moduleOwner;
            Debug.Log($"m_moduleOwner<{m_moduleOwner.GetType().ToString()}> is null? " + (m_moduleOwner != null).ToString());
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }
}
