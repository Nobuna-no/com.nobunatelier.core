using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/RotationModule Velocity Driven")]
    public class CharacterVelocityDrivenForwardRotation : CharacterRotationModuleBase
    {
        [SerializeField, Tooltip("Set to 0 the velocity axis you want to ignore.\n i.e. y=0 mean not using y velocity to orient the body.")]
        private Vector3 m_forwardSpace = Vector3.one;

        [SerializeField, Range(0, 50f)]
        private float m_rotationSpeed = 10f;

#if UNITY_EDITOR
        [SerializeField, ReadOnly]
        private Vector3 m_lastDirection;
#endif
        protected void SetForward(Vector3 dir, float deltaTime)
        {
            dir = Vector3.Slerp(ModuleOwner.transform.forward, dir, m_rotationSpeed * deltaTime);
            ModuleOwner.Body.Rotation = Quaternion.LookRotation(dir);
        }

        public override void RotationUpdate(float deltaTime)
        {
            Vector3 dir = ModuleOwner.GetMoveVector();

            dir.x = dir.x * m_forwardSpace.x;
            dir.y = dir.y * m_forwardSpace.y;
            dir.z = dir.z * m_forwardSpace.z;

#if UNITY_EDITOR
            m_lastDirection= dir;
#endif

            if (dir.sqrMagnitude <= 1)
            {
                return;
            }

            SetForward(dir.normalized, deltaTime);
        }
    }
}