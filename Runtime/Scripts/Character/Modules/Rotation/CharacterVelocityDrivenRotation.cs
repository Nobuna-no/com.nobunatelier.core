using System;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterVelocityDrivenRotation : CharacterRotationModule
    {
        [SerializeField]
        protected float m_rotationSpeed;

        protected void SetForward(Vector3 dir, float stepSpeed)
        {
            ModuleOwner.transform.forward = Vector3.Slerp(ModuleOwner.transform.forward, dir, stepSpeed);
        }

        public override void RotationUpdate(float deltaTime)
        {
            SetForward(ModuleOwner.GetMoveVector(), m_rotationSpeed * deltaTime);
        }
    }
}