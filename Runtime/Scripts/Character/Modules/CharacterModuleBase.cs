using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterModuleBase : MonoBehaviour
    {
        public AtelierCharacter ModuleOwner { get; private set; }
        public int Priority => m_priority;

        [SerializeField]
        private int m_priority = 0;

        public virtual void ModuleInit(AtelierCharacter character)
        {
            ModuleOwner = character;
        }

        public virtual void Reset()
        {

        }

        public virtual void StateUpdate(bool grounded)
        { }


        public virtual bool CanBeExecuted()
        {
            return isActiveAndEnabled;
        }
    }
}