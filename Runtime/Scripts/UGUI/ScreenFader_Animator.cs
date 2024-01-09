using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Utils/Screen Fader: Animation")]
    [RequireComponent(typeof(Animator))]
    public class ScreenFader_Animation : ScreenFader
    {
        [Header("Screen Fader: Animator")]
        [SerializeField, AnimatorParam("m_animator")] private string m_filledTrigger;

        [SerializeField, AnimatorParam("m_animator")] private string m_clearTrigger;

        [SerializeField] private AnimationClip m_fadeInAnimation;
        [SerializeField, AnimatorParam("m_animator")] private string m_fadeInTrigger;
        [SerializeField] private AnimationClip m_fadeOutAnimation;
        [SerializeField, AnimatorParam("m_animator")] private string m_fadeOutTrigger;

        public override bool IsFadeInProgress => m_fadeEstimatedRemainingTime != -1;

        private Animator m_animator;
        private float m_fadeEstimatedRemainingTime = -1;

        protected override void OnSingletonAwake()
        {
            m_animator = GetComponent<Animator>();
            AddAnimationEvent("FadeInEnd", m_fadeInAnimation.name);
            AddAnimationEvent("FadeOutEnd", m_fadeOutAnimation.name);
        }

        protected override void FillImpl()
        {
            m_fadeEstimatedRemainingTime = -1f;

            if (!m_animator)
            {
                return;
            }

            m_animator.SetTrigger(m_filledTrigger);
        }

        // Instantly fill the screen
        protected override void ClearImpl()
        {
            m_fadeEstimatedRemainingTime = -1f;

            if (!m_animator)
            {
                return;
            }

            m_animator.SetTrigger(m_clearTrigger);
        }

        protected override void FadeInImpl(float duration)
        {
            if (!m_animator)
            {
                return;
            }

            m_animator.speed = m_fadeInAnimation.length / duration;
            m_animator.SetTrigger(m_fadeInTrigger);

            m_fadeEstimatedRemainingTime = m_fadeInAnimation.length * duration;
        }

        protected override void FadeOutImpl(float duration)
        {
            if (!m_animator)
            {
                return;
            }

            m_animator.speed = m_fadeOutAnimation.length / duration;
            m_animator.SetTrigger(m_fadeOutTrigger);

            m_fadeEstimatedRemainingTime = m_fadeOutAnimation.length * duration;
        }

        protected override void FadeInEnd()
        {
            ResetFaderDuration();
            base.FadeInEnd();
        }

        protected override void FadeOutEnd()
        {
            ResetFaderDuration();
            base.FadeOutEnd();
        }

        private void ResetFaderDuration()
        {
            m_animator.speed = 1f;
        }

        private void AddAnimationEvent(string evtFunctionName, string targetAnimationName/*, out AnimationClip targetAnimationClip, out AnimationEvent targetAnimEvent*/)
        {
            AnimationEvent targetAnimEvent = null;
            AnimationClip targetAnimationClip = null;

            var clips = m_animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name != targetAnimationName)
                {
                    continue;
                }

                targetAnimationClip = clip;

                targetAnimEvent = new AnimationEvent();
                // Add event
                targetAnimEvent.time = clip.length;
                targetAnimEvent.functionName = evtFunctionName;
                targetAnimationClip.AddEvent(targetAnimEvent);
                return;
            }

            Debug.LogError($"No fade in animation clip named {m_fadeInAnimation.name} found in the animator.");
        }

        private void OnValidate()
        {
            m_animator = GetComponent<Animator>();
        }

        private void FixedUpdate()
        {
            if (m_fadeEstimatedRemainingTime == -1)
            {
                return;
            }

            m_fadeEstimatedRemainingTime -= Time.fixedDeltaTime;
            if (m_fadeEstimatedRemainingTime <= 0f)
            {
                m_fadeEstimatedRemainingTime = -1f;
            }
        }
    }
}