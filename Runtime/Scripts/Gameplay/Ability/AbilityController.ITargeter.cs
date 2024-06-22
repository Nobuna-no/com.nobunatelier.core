using NobunAtelier;
using UnityEngine;

namespace NobunAtelier
{
    public abstract partial class AbilityController : ITargeter
    {
        [Header("ITargeter")]
        [SerializeField] private Transform m_target;

        public Transform Target => m_target;

        public void SetTarget(ITargetable target)
        {
            m_target = target.TargetTransform;
        }

        public void SetTarget(Transform target)
        {
            m_target = target;
        }
    }
}