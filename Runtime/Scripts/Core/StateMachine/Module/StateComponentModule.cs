using UnityEngine;

namespace NobunAtelier
{
    public class StateComponentModule : MonoBehaviour
    {
        private StateComponent m_moduleOwner;
        public StateComponent ModuleOwner => m_moduleOwner;

        public virtual void Init(StateComponent moduleOwner)
        {
            m_moduleOwner = moduleOwner;
        }

        public virtual void Enter()
        { }

        public virtual void Tick(float deltaTime)
        { }

        public virtual void Exit()
        { }
    }
}