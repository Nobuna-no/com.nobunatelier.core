using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Rotation/RotationModule: Look At Target")]
    public class CharacterRotationToTarget : CharacterVelocityDrivenForwardRotation, ITargeter
    {
        [SerializeField]
        private Transform m_target;

        public void SetTarget(ITargetable target)
        {
            m_target = target.TargetTransform;
        }

        public void SetTarget(Transform target)
        {
            m_target = target;
        }

        public override void RotationUpdate(float deltaTime)
        {
            var dir = (m_target.position - ModuleOwner.Position).normalized;

            dir.x = dir.x * m_forwardSpace.x;
            dir.y = dir.y * m_forwardSpace.y;
            dir.z = dir.z * m_forwardSpace.z;

            SetForward(dir, deltaTime);
        }

        public override bool CanBeExecuted()
        {
            return this.isActiveAndEnabled && m_target != null;
        }
    }
}