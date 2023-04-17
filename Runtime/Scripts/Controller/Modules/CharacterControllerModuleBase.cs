using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public abstract class CharacterControllerModuleBase : MonoBehaviour
    {
        public abstract Character ControlledCharacter { get; }

        public abstract void InitModule(CharacterControllerBase controller);

        public virtual void UpdateModule(float deltaTime)
        {

        }

        public virtual bool IsAvailable()
        {
            return isActiveAndEnabled;
        }
    }

    public abstract class CharacterControllerModuleBase<T> : CharacterControllerModuleBase
        where T : CharacterControllerBase
    {
        public T ModuleOwner { get; private set; }
        public override Character ControlledCharacter => m_controlledCharacter;

        private Character m_controlledCharacter;

        public override void InitModule(CharacterControllerBase controller)
        {
            ModuleOwner = controller as T;
            m_controlledCharacter = controller.ControlledCharacter as Character;
        }
    }
}