using NobunAtelier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterRotationModuleBase : CharacterModuleBase
    {
        public virtual void RotateInput(Vector3 normalizedDirection) { }

        public abstract void RotationUpdate(float deltaTime);
    }
}