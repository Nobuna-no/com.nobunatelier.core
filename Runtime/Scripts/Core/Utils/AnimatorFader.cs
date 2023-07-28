using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public enum FadingMode
    {
        None,
        Normal,
        Instant
    }

    [RequireComponent(typeof(Animator))]
    public class AnimatorFader : SingletonManager<AnimatorFader>
    {
        [Header("Fader")]
        [SerializeField]
        private bool m_startFilled = false;

        [SerializeField, AnimatorParam("m_animator")]
        private string m_filledTrigger;

        [SerializeField, AnimatorParam("m_animator")]
        private string m_clearTrigger;

        [SerializeField]
        private AnimationClip m_fadeInAnimation;

        [SerializeField, AnimatorParam("m_animator")]
        private string m_fadeInTrigger;

        [SerializeField]
        private AnimationClip m_fadeOutAnimation;

        [SerializeField, AnimatorParam("m_animator")]
        private string m_fadeOutTrigger;

        [Header("Events")]
        public UnityEvent OnFadeInEnd;

        public UnityEvent OnFadeOutEnd;

        private Animator m_animator;
        private bool m_isFadeIn = false;

#if UNITY_EDITOR

        [Header("Debug")]
        [SerializeField]
        private bool m_logDebug = false;

#endif

        protected override AnimatorFader GetInstance()
        {
            return this;
        }

        [Button]
        public void FadeIn()
        {
            FadeIn(null);
        }

        [Button]
        public void FadeOut()
        {
            FadeOut(null);
        }

        // Instantly fill the screen
        public void Fill()
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Filling");
            }
#endif

            if (!m_animator || m_filledTrigger.Length == 0)
            {
                return;
            }

            m_animator.SetTrigger(m_filledTrigger);

            m_isFadeIn = true;
        }

        // Instantly fill the screen
        public void Clear()
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Clearing");
            }
#endif

            if (!m_animator || m_clearTrigger.Length == 0)
            {
                return;
            }

            m_animator.SetTrigger(m_clearTrigger);

            m_isFadeIn = false;
        }

        // Assumes that fade animation last 1 second.
        public void SetFaderDuration(float durationInSeconds)
        {
            m_animator.speed = 1f / durationInSeconds;
        }

        public void ResetFaderDuration()
        {
            m_animator.speed = 1f;
        }

        public void FadeIn(UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading in");
            }
#endif

            if (!m_animator || m_fadeInTrigger.Length == 0)
            {
                return;
            }

            if (m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            m_animator.SetTrigger(m_fadeInTrigger);
            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeInEnd.AddListener(actionToRaiseOnEnd);
        }

        public void FadeOut(UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading out");
            }
#endif

            if (!m_animator || m_fadeOutTrigger.Length == 0)
            {
                return;
            }

            if (!m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            m_animator.SetTrigger(m_fadeOutTrigger);
            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeOutEnd.AddListener(actionToRaiseOnEnd);
        }

        protected override void Awake()
        {
            base.Awake();

            m_animator = GetComponent<Animator>();
            AddAnimationEvent("FadeInEnd", m_fadeInAnimation.name);
            AddAnimationEvent("FadeOutEnd", m_fadeOutAnimation.name);
        }

        private void Start()
        {
            if (m_startFilled)
            {
                Fill();
            }
        }

        private void OnValidate()
        {
            m_animator = GetComponent<Animator>();
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

        private void FadeInEnd()
        {
            m_isFadeIn = true;
            OnFadeInEnd?.Invoke();
            OnFadeInEnd.RemoveAllListeners();
        }

        private void FadeOutEnd()
        {
            m_isFadeIn = false;
            OnFadeOutEnd?.Invoke();
            OnFadeOutEnd.RemoveAllListeners();
        }
    }
}