using NaughtyAttributes;
using System;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Rotation/RotationModule: Discrete")]
    public class CharacterDiscreteRotationModule : CharacterRotationModuleBase
    {
        [SerializeField, Tooltip("Set to 0 the velocity axis you want to ignore.\n i.e. y=0 mean not using y velocity to orient the body.")]
        private Axis m_targetAxis = Axis.Y;

        [SerializeField, Range(2, 32)]
        private int m_numDirections = 8;

        [SerializeField]
        private bool m_useMoveVector = false;

        [SerializeField, ReadOnly]
        private Vector3 m_lastInputDirection = Vector3.zero;

        public override void RotateInput(Vector3 normalizedDirection)
        {
            switch (m_targetAxis)
            {
                case Axis.X:
                    m_lastInputDirection.z = normalizedDirection.x;
                    m_lastInputDirection.y = normalizedDirection.y;
                    break;

                case Axis.Y:
                    m_lastInputDirection.x = normalizedDirection.x;
                    m_lastInputDirection.z = normalizedDirection.y;
                    break;

                case Axis.Z:
                    m_lastInputDirection.x = normalizedDirection.x;
                    m_lastInputDirection.y = normalizedDirection.y;
                    break;
            }
        }

        protected void SetForward(Vector3 dir, float deltaTime)
        {
            // Convert the rotation to Euler angles
            Vector3 currentEulerAngles = Quaternion.LookRotation(dir).eulerAngles;

            // Calculate the angle step based on the number of directions
            float angleStep = 360f / m_numDirections;

            // Round the angle to the nearest step
            switch (m_targetAxis)
            {
                case Axis.X:
                    currentEulerAngles.x = Mathf.Round(currentEulerAngles.x / angleStep) * angleStep;
                    break;

                case Axis.Y:
                    currentEulerAngles.y = Mathf.Round(currentEulerAngles.y / angleStep) * angleStep;
                    break;

                case Axis.Z:
                    currentEulerAngles.z = Mathf.Round(currentEulerAngles.z / angleStep) * angleStep;
                    break;
            }

            ModuleOwner.Body.Rotation = Quaternion.Euler(currentEulerAngles);
        }

        public override void RotationUpdate(float deltaTime)
        {
            Vector3 dir;
            if (m_useMoveVector)
            {
                dir = ModuleOwner.GetMoveVector();
            }
            else
            {
                dir = m_lastInputDirection;
            }

            if (dir == Vector3.zero)
            {
                return;
            }

            SetForward(dir.normalized, deltaTime);
        }

        [Serializable]
        private enum Axis
        {
            X, Y, Z
        }
    }
}