using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public abstract class CharacterModuleBase : MonoBehaviour
    {
        public Character ModuleOwner { get; private set; }
        public int Priority => m_Priority;

        [SerializeField]
        [FormerlySerializedAs("m_priority")]
        private int m_Priority = 0;

        public virtual void ModuleInit(Character character)
        {
            ModuleOwner = character;
        }

        public virtual void ModuleStop()
        {
        }

        public virtual void Reset()
        { }

        public virtual bool CanBeExecuted()
        {
            return isActiveAndEnabled;
        }
    }
}