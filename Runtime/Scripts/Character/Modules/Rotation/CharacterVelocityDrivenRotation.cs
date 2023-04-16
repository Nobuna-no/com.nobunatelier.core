using System;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterVelocityDrivenRotation : CharacterRotationModule
    {
        [SerializeField, Range(0, 50f)]
        protected float m_rotationSpeed = 10f;

        protected void SetForward(Vector3 dir, float stepSpeed)
        {
            dir.y = 0;
            ModuleOwner.transform.forward = Vector3.Slerp(ModuleOwner.transform.forward, dir, stepSpeed);
        }

        public override void RotationUpdate(float deltaTime)
        {
            if (ModuleOwner.GetMoveVector() == Vector3.zero)
            {
                return;
            }

            SetForward(ModuleOwner.GetMoveVector(), m_rotationSpeed * deltaTime);
        }
    }
}