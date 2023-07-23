using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [DefaultExecutionOrder(10)]
    public class Target : MonoBehaviour, ITargetable
    {
        //[SerializeField, Required]
        //private Renderer m_Renderer;

        [SerializeField]
        private bool m_isTargetable = true;

        // private bool m_isVisible = false;

        public bool IsTargetable => m_isTargetable;

        public Transform Transform => transform;

        public Vector3 Position => transform.position;


        public void OnEnable()
        {
            // Debug.Assert(m_Renderer);
            TargetManager.Instance.Register(this);
        }

        public void OnDisable()
        {
            TargetManager.Instance.Unregister(this);
        }

        // Forget about rendering as if we kill someone and nobody is visible, then we are soft locked...
        //private void LateUpdate()
        //{
        //    if (m_Renderer)
        //    {
        //        m_isVisible = m_Renderer.isVisible;
        //    }
        //}
    }
}