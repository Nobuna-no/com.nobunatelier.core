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

        [SerializeField] private string m_fillStateName;
        [SerializeField] private string m_clearStateName;

        [Header("Audio")]
        [SerializeField] private float m_audioStartDelayInSecond = 0.2f;

        [SerializeField] private AudioSource m_FillAudioSource;
        [SerializeField] private AudioSource m_fadeInAudioSource;
        [SerializeField] private AudioSource m_fadeOutAudioSource;

        [Header("Events")]
        [Tooltip("Can be use to set the Image RaycastTarget to true to block UI on fade start. Don't forget to do the opposite in OnFadeOutEnd")]
        public UnityEvent OnFadeInBegin;

        public UnityEvent OnFadeInEnd;
        public UnityEvent OnFadeOutBegin;
        public UnityEvent OnFadeOutEnd;
        public bool IsFadeIn { get; private set; } = false;

        private Animator m_animator;
        private float m_currentTime = 0f;
        private int m_fillStateHash;
        private int m_clearStateHash;
        private bool m_isCrossFadingIn = false;

#if UNITY_EDITOR

        [Header("Debug")]
        [SerializeField]
        private float m_debugCrossFadeDuration = 1f;

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
            if (!m_animator)
            {
                return;
            }

            m_animator.CrossFadeInFixedTime(m_fillStateName, 0, 0);

            IsFadeIn = true;
            m_isCrossFadingIn = true;

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

            if (!m_animator)
            {
                return;
            }

            m_animator.CrossFadeInFixedTime(m_clearStateName, 0, 0);

            IsFadeIn = false;
        }

        public void ResetFaderDuration()
        {
            m_animator.speed = 1f;
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void FadeIn()
        {
            FadeIn(m_debugCrossFadeDuration, null);
        }

#endif

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

            if (!m_animator)
            {
                return;
            }

            if (m_currentTime != -1)
            {
                if (m_isCrossFadingIn)
                {
                    // force finish previous fade in and start new one.
                    FadeInEnd();
                    IsFadeIn = false;
                }
                else
                {
                    FadeOutEnd();
                }
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

            m_animator.CrossFadeInFixedTime(m_fillStateHash, duration, 0);
            m_currentTime = duration;
            m_isCrossFadingIn = true;
            OnFadeInBegin?.Invoke();

            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeInEnd.AddListener(actionToRaiseOnEnd);
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void FadeOut()
        {
            FadeOut(m_debugCrossFadeDuration, null);
        }

#endif

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

            if (!m_animator)
            {
                return;
            }

            if (m_currentTime != -1)
            {
                if (!m_isCrossFadingIn)
                {
                    // force finish previous fade out and start new one.
                    FadeOutEnd();
                    IsFadeIn = true;
                }
                else
                {
                    FadeInEnd();
                }
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

            m_animator.CrossFadeInFixedTime(m_clearStateHash, duration, 0);
            m_currentTime = duration;
            m_isCrossFadingIn = false;
            OnFadeOutBegin?.Invoke();

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
            m_fillStateHash = Animator.StringToHash(m_fillStateName);
            m_clearStateHash = Animator.StringToHash(m_clearStateName);

            Debug.Assert(m_animator.HasState(0, m_fillStateHash), $"{this.GetType().Name}: Cannot find valid Fill state name '{m_fillStateName}' in AnimatorController.");
            Debug.Assert(m_animator.HasState(0, m_clearStateHash), $"{this.GetType().Name}: Cannot find valid Fill state name '{m_clearStateName}' in AnimatorController.");
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

                if (m_isCrossFadingIn)
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

// Legacy code but might be handy in the future
//private void AddAnimationEvent(string evtFunctionName, string targetAnimationName/*, out AnimationClip targetAnimationClip, out AnimationEvent targetAnimEvent*/)
//{
//    AnimationEvent targetAnimEvent = null;
//    AnimationClip targetAnimationClip = null;

//    var clips = m_animator.runtimeAnimatorController.animationClips;
//    foreach (var clip in clips)
//    {
//        if (clip.name != targetAnimationName)
//        {
//            continue;
//        }

//        targetAnimationClip = clip;

//        targetAnimEvent = new AnimationEvent();
//        // Add event
//        targetAnimEvent.time = clip.length;
//        targetAnimEvent.functionName = evtFunctionName;
//        targetAnimationClip.AddEvent(targetAnimEvent);
//        return;
//    }

//    Debug.LogError($"No fade in animation clip named {m_fadeInAnimation.name} found in the animator.");
//}