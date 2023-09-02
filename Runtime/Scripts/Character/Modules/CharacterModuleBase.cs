using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterModuleBase : MonoBehaviour
    {
        public Character ModuleOwner { get; private set; }
        public int Priority => m_priority;

        [SerializeField]
        private int m_priority = 0;

        public virtual void ModuleInit(Character character)
        {
            ModuleOwner = character;
        }

        public virtual void ModuleStop()
        {
        }

        public virtual void Reset()
        { }

        public virtual void StateUpdate(bool grounded)
        { }

        public virtual bool CanBeExecuted()
        {
            return isActiveAndEnabled;
        }
    }
}