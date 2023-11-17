using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Utils/Screen Fader/Animation")]
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

        private Animator m_animator;

        protected override void Awake()
        {
            base.Awake();

            m_animator = GetComponent<Animator>();
            AddAnimationEvent("FadeInEnd", m_fadeInAnimation.name);
            AddAnimationEvent("FadeOutEnd", m_fadeOutAnimation.name);
        }

        protected override void FillImpl()
        {
            base.FillImpl();

            if (!m_animator)
            {
                return;
            }

            m_animator.SetTrigger(m_filledTrigger);
        }

        // Instantly fill the screen
        protected override void ClearImpl()
        {
            base.ClearImpl();

            if (!m_animator)
            {
                return;
            }

            m_animator.SetTrigger(m_clearTrigger);
        }

        protected override void FadeInImpl(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            if (!m_animator)
            {
                base.FadeInImpl(duration, actionToRaiseOnEnd);
                return;
            }

            SetFaderDuration(duration);

            m_animator.SetTrigger(m_fadeInTrigger);

            base.FadeInImpl(duration, actionToRaiseOnEnd);
        }

        protected override void FadeOutImpl(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            if (!m_animator)
            {
                base.FadeOutImpl(duration, actionToRaiseOnEnd);
                return;
            }

            SetFaderDuration(duration);
            m_animator.SetTrigger(m_fadeOutTrigger);

            base.FadeOutImpl(duration, actionToRaiseOnEnd);
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

        private void SetFaderDuration(float duration)
        {
            m_animator.speed = m_fadeInAnimation.length / duration;
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
    }
}