using System;
using UnityEngine;

namespace NobunAtelier
{
    // Better use the rigidbody.gravity instead...
    [AddComponentMenu("NobunAtelier/Character/VelocityModule LockAxis")]
    public class CharacterLockAxisVelocity : CharacterVelocityModuleBase
    {
        [System.Flags]
        public enum LockAxis
        {
            X = 1,
            Y = 2,
            Z = 4
        }

        [SerializeField]
        private LockAxis m_lockAxes = LockAxis.Y;

        [SerializeField]
        private Vector3 m_axesValue = Vector3.one;

        [SerializeField, Range(0, 100)]
        private float m_lerpSpeed = 1f;

        public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
        {
            Vector3 dest = ModuleOwner.Position;

            if ((m_lockAxes & LockAxis.X) != 0)
            {
                currentVel.x = m_axesValue.x;
            }
            if ((m_lockAxes & LockAxis.Y) != 0)
            {
                currentVel.y = m_lerpSpeed == 0 ? (m_axesValue.y - dest.y) / deltaTime  : (m_axesValue.y - dest.y) * m_lerpSpeed;
                // currentVel.y = m_axesValue.y;
            }
            if ((m_lockAxes & LockAxis.Z) != 0)
            {
                currentVel.z = m_axesValue.z;
            }

            return currentVel;
        }
    }
}