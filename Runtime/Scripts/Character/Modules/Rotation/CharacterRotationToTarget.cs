using System;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterRotationToTarget : CharacterVelocityDrivenRotation
    {
        [SerializeField]
        private Transform m_target;

        public override void RotationUpdate(float deltaTime)
        {
            var dir = (m_target.position - ModuleOwner.Position).normalized;

            SetForward(dir, m_rotationSpeed);
        }

        public override bool CanBeExecuted()
        {
            return m_target != null;
        }
    }
}