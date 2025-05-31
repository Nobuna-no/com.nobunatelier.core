using UnityEngine;

namespace NobunAtelier
{
    public class StateComponentModule : MonoBehaviour
    {
        private StateComponent m_ModuleOwner;
        public StateComponent ModuleOwner => m_ModuleOwner;

        public virtual void Init(StateComponent moduleOwner)
        {
            m_ModuleOwner = moduleOwner;
        }

        public virtual void Enter()
        { }

        public virtual void Tick(float deltaTime)
        { }

        public virtual void Exit()
        { }
    }
}