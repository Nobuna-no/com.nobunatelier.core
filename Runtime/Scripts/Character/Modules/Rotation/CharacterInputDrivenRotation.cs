using System;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Rotation/RotationModule: Input Driven")]
    public class CharacterInputDrivenRotation : CharacterRotationModuleBase
    {
        [Serializable]
        public enum RotationAxis
        {
            X,
            Y,
            Z
        }

        [SerializeField]
        private RotationAxis m_rotationAxis = RotationAxis.Y;
        [SerializeField, Range(0, 10f)]
        private float m_rotationSpeed = 1f;

        private Vector3 m_lastDirection;

        public override void RotateInput(Vector3 normalizedDirection)
        {
            m_lastDirection = normalizedDirection;
        }

        public override void RotationUpdate(float deltaTime)
        {
            var rot = TowDownDirectionToQuaternion(m_lastDirection);
            ModuleOwner.transform.rotation = Quaternion.Slerp(ModuleOwner.transform.rotation, rot, m_rotationSpeed * deltaTime);
        }

        private Quaternion TowDownDirectionToQuaternion(Vector3 normalizedDirection)
        {
            switch (m_rotationAxis)
            {
                case RotationAxis.X:
                    return Quaternion.Euler(new Vector3(Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg, 0, 0));

                case RotationAxis.Y:
                    return Quaternion.Euler(new Vector3(0, Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg, 0));

                case RotationAxis.Z:
                    return Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg));
            }

            return Quaternion.identity;
        }
    }
}