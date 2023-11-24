using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace NobunAtelier.Gameplay
{
    public class HitboxWithIndicatorBehaviour : HitboxBehaviour
    {
        [SerializeField]
        private Gradient m_warningGradient;

        [SerializeField]
        private Color m_hitIndicatorGradient = Color.red;

        private MeshRenderer m_renderer;

        private float m_warningDuration = 1f;
        private float m_currentTime = 0f;

        public virtual void StartWarningAnimation(float duration)
        {
            m_warningDuration = duration;
            m_currentTime = 0;
            m_renderer.material.color = m_warningGradient.Evaluate(0);
            m_renderer.enabled = true;
            this.enabled = true;
        }

        public virtual void CancelWarningAndHide()
        {
            m_currentTime = 0f;
            this.enabled = false;
            m_renderer.enabled = false;
        }

        public virtual void HitIndicatorStart()
        {
            m_warningDuration = 0;
            this.enabled = false;

            m_renderer.enabled = true;
            m_renderer.material.color = m_hitIndicatorGradient;
        }

        public virtual void HitIndicatorStop()
        {
            m_renderer.enabled = false;
        }

        protected override void Awake()
        {
            m_renderer = GetComponentInChildren<MeshRenderer>();
            m_renderer.enabled = false;

            base.Awake();
        }

        public override void HitBegin()
        {
            base.HitBegin();
            HitIndicatorStart();
        }

        public override void HitEnd()
        {
            base.HitEnd();
            HitIndicatorStop();
        }

        protected virtual void Start()
        {
            this.enabled = false;
        }

        private void Update()
        {
            m_currentTime += Time.deltaTime;
            m_renderer.material.color = m_warningGradient.Evaluate(m_currentTime / m_warningDuration);
        }

#if UNITY_EDITOR

        [Button]
        private void ToggleRenderer()
        {
            m_renderer = GetComponentInChildren<MeshRenderer>();
            m_renderer.enabled = !m_renderer.enabled;
        }

#endif
    }
}