using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterAbilityModuleBase : CharacterModuleBase
    {
        public virtual void AbilityUpdate(float deltaTime) { }
    }
}
