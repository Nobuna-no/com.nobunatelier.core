using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Utils/Screen Fader: Animation")]
    [RequireComponent(typeof(Animator))]
    public class ScreenFader_Animation : ScreenFader
    {
        [Header("Screen Fader: Animator")]
        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_filledTrigger")]
        private string m_FilledTrigger;

        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_clearTrigger")]
        private string m_ClearTrigger;

        [SerializeField, FormerlySerializedAs("m_fadeInAnimation")]
        private AnimationClip m_FadeInAnimation;
        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_fadeInTrigger")]
        private string m_FadeInTrigger;
        [SerializeField, FormerlySerializedAs("m_fadeOutAnimation")]
        private AnimationClip m_FadeOutAnimation;
        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_fadeOutTrigger")]
        private string m_FadeOutTrigger;

        public override bool IsFadeInProgress => m_FadeEstimatedRemainingTime != -1;

        private Animator m_Animator;
        private float m_FadeEstimatedRemainingTime = -1;

        protected override void OnSingletonAwake()
        {
            m_Animator = GetComponent<Animator>();
            AddAnimationEvent("FadeInEnd", m_FadeInAnimation.name);
            AddAnimationEvent("FadeOutEnd", m_FadeOutAnimation.name);
        }

        protected override void FillImpl()
        {
            m_FadeEstimatedRemainingTime = -1f;

            if (!m_Animator)
            {
                return;
            }

            m_Animator.SetTrigger(m_FilledTrigger);
        }

        // Instantly fill the screen
        protected override void ClearImpl()
        {
            m_FadeEstimatedRemainingTime = -1f;

            if (!m_Animator)
            {
                return;
            }

            m_Animator.SetTrigger(m_ClearTrigger);
        }

        protected override void FadeInImpl(float duration)
        {
            if (!m_Animator)
            {
                return;
            }

            m_Animator.speed = m_FadeInAnimation.length / duration;
            m_Animator.SetTrigger(m_FadeInTrigger);

            m_FadeEstimatedRemainingTime = m_FadeInAnimation.length * duration;
        }

        protected override void FadeOutImpl(float duration)
        {
            if (!m_Animator)
            {
                return;
            }

            m_Animator.speed = m_FadeOutAnimation.length / duration;
            m_Animator.SetTrigger(m_FadeOutTrigger);

            m_FadeEstimatedRemainingTime = m_FadeOutAnimation.length * duration;
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
            m_Animator.speed = 1f;
        }

        private void AddAnimationEvent(string evtFunctionName, string targetAnimationName/*, out AnimationClip targetAnimationClip, out AnimationEvent targetAnimEvent*/)
        {
            AnimationEvent targetAnimEvent = null;
            AnimationClip targetAnimationClip = null;

            var clips = m_Animator.runtimeAnimatorController.animationClips;
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

            Debug.LogError($"No fade in animation clip named {m_FadeInAnimation.name} found in the animator.");
        }

        private void OnValidate()
        {
            m_Animator = GetComponent<Animator>();
        }

        private void FixedUpdate()
        {
            if (m_FadeEstimatedRemainingTime == -1)
            {
                return;
            }

            m_FadeEstimatedRemainingTime -= Time.fixedDeltaTime;
            if (m_FadeEstimatedRemainingTime <= 0f)
            {
                m_FadeEstimatedRemainingTime = -1f;
            }
        }
    }
}