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
    public class ScreenFader : SingletonManager<ScreenFader>
    {
        [Header("Fader")]
        [SerializeField] private bool m_startFilled = false;

        [SerializeField, AnimatorParam("m_animator")] private string m_filledTrigger;
        [SerializeField, AnimatorParam("m_animator")] private string m_clearTrigger;

        [SerializeField] private AnimationClip m_fadeInAnimation;
        [SerializeField, AnimatorParam("m_animator")] private string m_fadeInTrigger;
        [SerializeField] private AnimationClip m_fadeOutAnimation;
        [SerializeField, AnimatorParam("m_animator")] private string m_fadeOutTrigger;

        [Header("Experimental")]
        [SerializeField] private bool m_useExperimentalCrossFade = false;
        [SerializeField, ShowIf("m_useExperimentalCrossFade")] private string m_fillStateName;
        [SerializeField, ShowIf("m_useExperimentalCrossFade")] private string m_clearStateName;
        [Tooltip("Can be use to set the Image RaycastTarget to true to block UI on fade start. Don't forget to do the opposite in OnFadeOutEnd")]
        [ShowIf("m_useExperimentalCrossFade")] public UnityEvent OnFadeInBegin;
        [ShowIf("m_useExperimentalCrossFade")] public UnityEvent OnFadeOutBegin;

        [Header("Audio")]
        [SerializeField] private float m_audioStartDelayInSecond = 0.2f;

        [SerializeField] private AudioSource m_FillAudioSource;
        [SerializeField] private AudioSource m_fadeInAudioSource;
        [SerializeField] private AudioSource m_fadeOutAudioSource;

        [Header("Events")]
        public UnityEvent OnFadeInEnd;
        public UnityEvent OnFadeOutEnd;

        public bool IsFadeIn { get; private set; } = false;

        private Animator m_animator;
        private float m_currentTime = 0f;
        private bool m_isCrossFading = false;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField]
        private bool m_logDebug = false;
#endif

        protected override ScreenFader GetInstance()
        {
            return this;
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

            if (m_useExperimentalCrossFade)
            {
                m_animator.CrossFadeInFixedTime(m_fillStateName, 0, 0);
            }
            else
            {
                m_animator.SetTrigger(m_filledTrigger);
            }

            IsFadeIn = true;

            if (m_FillAudioSource)
            {
                m_FillAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }
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

            if (m_useExperimentalCrossFade)
            {
                m_animator.CrossFadeInFixedTime(m_clearStateName, 0, 0);
            }
            else
            {
                m_animator.SetTrigger(m_clearTrigger);
            }

            IsFadeIn = false;
        }

        public void ResetFaderDuration()
        {
            m_animator.speed = 1f;
        }

        [Button]
        public void FadeIn()
        {
            FadeIn(null);
        }

        public void FadeIn(UnityAction actionToRaiseOnEnd = null)
        {
            FadeIn(0, actionToRaiseOnEnd);
        }

        public void FadeIn(float duration, UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading in for {duration} sec.", this);
            }
#endif

            if (!m_animator || m_fadeInTrigger.Length == 0)
            {
                return;
            }

            if (m_isCrossFading)
            {
                // force finish previous fade in and start new one.
                FadeInEnd();
                IsFadeIn = false;
            }

            if (IsFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            if (m_fadeInAudioSource)
            {
                m_fadeInAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }

            if (m_useExperimentalCrossFade)
            {
                m_animator.CrossFadeInFixedTime(m_fillStateName, duration, 0);
                m_currentTime = duration;
                m_isCrossFading = true;
                OnFadeInBegin?.Invoke();
            }
            else
            {
                m_animator.SetTrigger(m_fadeInTrigger);
            }

            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeInEnd.AddListener(actionToRaiseOnEnd);
        }

        [Button]
        public void FadeOut()
        {
            FadeOut(null);
        }

        public void FadeOut(UnityAction actionToRaiseOnEnd = null)
        {
            FadeOut(0, actionToRaiseOnEnd);
        }

        public void FadeOut(float duration, UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading out for {duration} sec.");
            }
#endif

            if (!m_animator || m_fadeOutTrigger.Length == 0)
            {
                return;
            }

            if (!m_isCrossFading)
            {
                // force finish previous fade out and start new one.
                FadeOutEnd();
                IsFadeIn = true;
            }

            if (!IsFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            if (m_fadeOutAudioSource)
            {
                m_fadeOutAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }

            if (m_useExperimentalCrossFade)
            {
                m_animator.CrossFadeInFixedTime(m_clearStateName, duration, 0);
                m_currentTime = duration;
                m_isCrossFading = false;
                OnFadeOutBegin?.Invoke();
            }
            else
            {
                m_animator.SetTrigger(m_fadeOutTrigger);
            }

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
            // AddAnimationEvent("FadeInEnd", m_fadeInAnimation.name);
            // AddAnimationEvent("FadeOutEnd", m_fadeOutAnimation.name);
        }

        private void Start()
        {
            if (m_startFilled)
            {
                Fill();
            }
        }

        private void FixedUpdate()
        {
            if (m_currentTime == -1)
            {
                return;
            }

            m_currentTime -= Time.fixedDeltaTime;
            if (m_currentTime < 0f)
            {
                m_currentTime = -1f;

                if (m_isCrossFading)
                {
                    FadeInEnd();
                }
                else
                {
                    FadeOutEnd();
                }
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
            IsFadeIn = true;
            OnFadeInEnd?.Invoke();
            OnFadeInEnd.RemoveAllListeners();
        }

        private void FadeOutEnd()
        {
            IsFadeIn = false;
            OnFadeOutEnd?.Invoke();
            OnFadeOutEnd.RemoveAllListeners();
        }
    }
}