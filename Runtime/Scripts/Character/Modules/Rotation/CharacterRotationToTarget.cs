using System;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character Module/Rotation To Target")]
    public class CharacterRotationToTarget : CharacterVelocityDrivenForwardRotation
    {
        [SerializeField]
        private Transform m_target;

        public override void RotationUpdate(float deltaTime)
        {
            var dir = (m_target.position - ModuleOwner.Position).normalized;

            SetForward(dir, deltaTime);
        }

        public override bool CanBeExecuted()
        {
            return m_target != null;
        }
    }
}