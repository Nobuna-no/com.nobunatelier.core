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

    public abstract class ScreenFader : Singleton<ScreenFader>
    {
        [Header("Fader")]
        [SerializeField] protected bool m_startFilled = false;

        // [SerializeField] protected string m_fillStateName;
        // [SerializeField] protected string m_clearStateName;
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
        protected bool m_isFadeIn;

        // private Animator m_animator;
        // private float m_currentTime = 0f;
        // private int m_fillStateHash;
        // private int m_clearStateHash;
        // private bool m_isCrossFadingIn = false;

#if UNITY_EDITOR

        // [Header("Debug")]
        // [SerializeField]
        // private float m_debugCrossFadeDuration = 1f;

        [SerializeField]
        private bool m_logDebug = false;

#endif

        protected virtual void Start()
        {
            if (m_startFilled)
            {
                Fill();
            }
        }

        public static void Fill()
        {
            Instance.FillImpl();
        }

        public static void Clear()
        {
            Instance.ClearImpl();
        }

        public static void FadeIn(UnityAction actionToRaiseOnEnd = null)
        {
            Instance.FadeInImpl(actionToRaiseOnEnd);
        }

        public static void FadeIn(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            Instance.FadeInImpl(duration, actionToRaiseOnEnd);
        }

        public static void FadeOut(UnityAction actionToRaiseOnEnd = null)
        {
            Instance.FadeOutImpl(actionToRaiseOnEnd);
        }

        public static void FadeOut(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            Instance.FadeOutImpl(duration, actionToRaiseOnEnd);
        }

        public static bool IsFadeIn()
        {
            return Instance.m_isFadeIn;
        }

        // Instantly fill the screen
        protected virtual void FillImpl()
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Filling");
            }
#endif
            //if (!m_animator)
            //{
            //    return;
            //}

            //m_animator.CrossFadeInFixedTime(m_fillStateName, 0, 0);

            m_isFadeIn = true;
            //m_isCrossFadingIn = true;

            if (m_FillAudioSource)
            {
                m_FillAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }
        }

        // Instantly fill the screen
        protected virtual void ClearImpl()
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Clearing");
            }
#endif

            //if (!m_animator)
            //{
            //    return;
            //}

            //m_animator.CrossFadeInFixedTime(m_clearStateName, 0, 0);

            m_isFadeIn = false;
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void Editor_FadeIn()
        {
            FadeIn(1);
        }

#endif

        protected virtual void FadeInImpl(UnityAction actionToRaiseOnEnd = null)
        {
            FadeIn(0, actionToRaiseOnEnd);
        }

        protected virtual void FadeInImpl(float duration, UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading in for {duration} sec.", this);
            }
#endif

            //if (!m_animator)
            //{
            //    return;
            //}

            //if (m_currentTime != -1)
            //{
            //    if (m_isCrossFadingIn)
            //    {
            //        // force finish previous fade in and start new one.
            //        FadeInEnd();
            //        IsFadeIn = false;
            //    }
            //    else
            //    {
            //        FadeOutEnd();
            //    }
            //}

            if (m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            if (m_fadeInAudioSource)
            {
                m_fadeInAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }

            // m_animator.CrossFadeInFixedTime(m_fillStateHash, duration, 0);
            // m_currentTime = duration;
            // m_isCrossFadingIn = true;
            OnFadeInBegin?.Invoke();

            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeInEnd.AddListener(actionToRaiseOnEnd);
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void Editor_FadeOut()
        {
            FadeOut(1);
        }

#endif

        protected virtual void FadeOutImpl(UnityAction actionToRaiseOnEnd = null)
        {
            FadeOut(0, actionToRaiseOnEnd);
        }

        protected virtual void FadeOutImpl(float duration, UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading out for {duration} sec.");
            }
#endif

            //if (!m_animator)
            //{
            //    return;
            //}

            //if (m_currentTime != -1)
            //{
            //    if (!m_isCrossFadingIn)
            //    {
            //        // force finish previous fade out and start new one.
            //        FadeOutEnd();
            //        IsFadeIn = true;
            //    }
            //    else
            //    {
            //        FadeInEnd();
            //    }
            //}

            if (!m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            if (m_fadeOutAudioSource)
            {
                m_fadeOutAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }

            //m_animator.CrossFadeInFixedTime(m_clearStateHash, duration, 0);
            //m_currentTime = duration;
            //m_isCrossFadingIn = false;
            OnFadeOutBegin?.Invoke();

            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeOutEnd.AddListener(actionToRaiseOnEnd);
        }

        //protected override void Awake()
        //{
        //    base.Awake();

        //    // m_animator = GetComponent<Animator>();
        //    // m_fillStateHash = Animator.StringToHash(m_fillStateName);
        //    // m_clearStateHash = Animator.StringToHash(m_clearStateName);
        //    //
        //    // Debug.Assert(m_animator.HasState(0, m_fillStateHash), $"{this.GetType().Name}: Cannot find valid Fill state name '{m_fillStateName}' in AnimatorController.");
        //    // Debug.Assert(m_animator.HasState(0, m_clearStateHash), $"{this.GetType().Name}: Cannot find valid Fill state name '{m_clearStateName}' in AnimatorController.");
        //}

        //private void FixedUpdate()
        //{
        //    if (m_currentTime == -1)
        //    {
        //        return;
        //    }

        //    m_currentTime -= Time.fixedDeltaTime;
        //    if (m_currentTime < 0f)
        //    {
        //        m_currentTime = -1f;

        //        if (m_isCrossFadingIn)
        //        {
        //            FadeInEnd();
        //        }
        //        else
        //        {
        //            FadeOutEnd();
        //        }
        //    }
        //}

        //private void OnValidate()
        //{
        //    m_animator = GetComponent<Animator>();
        //}

        protected virtual void FadeInEnd()
        {
            m_isFadeIn = true;
            OnFadeInEnd?.Invoke();
            OnFadeInEnd.RemoveAllListeners();
        }

        protected virtual void FadeOutEnd()
        {
            m_isFadeIn = false;
            OnFadeOutEnd?.Invoke();
            OnFadeOutEnd.RemoveAllListeners();
        }
    }
}