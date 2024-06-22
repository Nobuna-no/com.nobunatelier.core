using UnityEngine;

namespace NobunAtelier
{
    [DefaultExecutionOrder(10)]
    public class TargetBehaviour : MonoBehaviour, ITargetable
    {
        [SerializeField]
        private bool m_isTargetable = true;

        public bool IsTargetable => m_isTargetable && this.enabled;

        public Transform TargetTransform => transform;

        public Vector3 Position => transform.position;

        public void OnEnable()
        {
            TargetManager.Instance.Register(this);
        }

        public void OnDisable()
        {
            if (TargetManager.IsSingletonValid)
            {
                TargetManager.Instance.Unregister(this);
            }
        }
    }
}