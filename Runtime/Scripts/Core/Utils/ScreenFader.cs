using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    /// <summary>
    /// An abstract class responsible for managing screen fading effects.
    /// It also has events that are triggered when a fade in or fade out begins or ends.
    /// The abstraction allows to implement the fading effect, using an animator or a shader for example.
    /// </summary>
    public abstract class ScreenFader : Singleton<ScreenFader>
    {
        public enum FadingMode
        {
            None,
            Normal,
            Instant
        }

        [Header("Fader")]
        [SerializeField] protected bool m_startFilled = false;

        [Header("Audio")]
        [SerializeField] private float m_audioStartDelayInSecond = 0.2f;

        [SerializeField] private AudioSource m_fillAudioSource;
        [SerializeField] private AudioSource m_clearAudioSource;
        [SerializeField] private AudioSource m_fadeInAudioSource;
        [SerializeField] private AudioSource m_fadeOutAudioSource;

        [Header("Events")]
        [Tooltip("Can be use to set the Image RaycastTarget to true to block UI on fade start. Don't forget to do the opposite in OnFadeOutEnd")]
        public UnityEvent OnFadeInBegin;

        public abstract bool IsFadeInProgress { get; }

        public UnityEvent OnFadeInEnd;
        public UnityEvent OnFadeOutBegin;
        public UnityEvent OnFadeOutEnd;
        protected bool m_isFadeIn;

        protected virtual void Start()
        {
            if (m_startFilled)
            {
                Internal_Fill();
            }
        }

        public static void Fill()
        {
            Instance.Internal_Fill();
        }

        public static void Clear()
        {
            Instance.Internal_Clear();
        }

        public static void FadeIn(UnityAction actionToRaiseOnEnd = null)
        {
            Instance.Internal_FadeIn(0, actionToRaiseOnEnd);
        }

        public static void FadeIn(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            Instance.Internal_FadeIn(duration, actionToRaiseOnEnd);
        }

        public static void FadeOut(UnityAction actionToRaiseOnEnd = null)
        {
            Instance.Internal_FadeOut(0, actionToRaiseOnEnd);
        }

        public static void FadeOut(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            Instance.Internal_FadeOut(duration, actionToRaiseOnEnd);
        }

        public static bool IsFadeIn()
        {
            return Instance.m_isFadeIn;
        }

        protected abstract void FillImpl();

        protected abstract void ClearImpl();

        protected abstract void FadeInImpl(float duration);

        protected abstract void FadeOutImpl(float duration);

        private void Internal_Fill()
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Filling");
            }
#endif

            if (m_isFadeIn)
            {
                return;
            }

            FillImpl();

            m_isFadeIn = true;

            if (m_fillAudioSource)
            {
                m_fillAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }
        }

        private void Internal_Clear()
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Clearing");
            }
#endif

            if (!m_isFadeIn)
            {
                return;
            }

            ClearImpl();

            m_isFadeIn = false;

            if (m_clearAudioSource)
            {
                m_clearAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }
        }

        private void Internal_FadeIn(float duration, UnityAction actionToRaiseOnEnd = null)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading in for {duration} sec.", this);
            }
#endif

            // If already faded in, just invoke the callback.
            if (m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            // If already fading in, just add the callback to the event.
            if (IsFadeInProgress)
            {
                if (actionToRaiseOnEnd != null)
                {
                    OnFadeInEnd?.AddListener(actionToRaiseOnEnd);
                }
                return;
            }

            // Start fading
            FadeInImpl(duration);

            OnFadeInBegin?.Invoke();

            if (m_fadeInAudioSource)
            {
                m_fadeInAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }

            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeInEnd.AddListener(actionToRaiseOnEnd);
        }

        private void Internal_FadeOut(float duration, UnityAction actionToRaiseOnEnd)
        {
#if UNITY_EDITOR
            if (m_logDebug)
            {
                Debug.Log($"[{Time.frameCount}] - Fading out for {duration} sec.");
            }
#endif

            // If already faded out, just invoke the callback.
            if (!m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            // If already fading out, just add the callback to the event.
            if (IsFadeInProgress)
            {
                if (actionToRaiseOnEnd != null)
                {
                    OnFadeOutBegin?.AddListener(actionToRaiseOnEnd);
                }
                return;
            }

            // Start fading
            FadeOutImpl(duration);

            OnFadeOutBegin?.Invoke();

            if (m_fadeOutAudioSource)
            {
                m_fadeOutAudioSource.PlayScheduled(AudioSettings.dspTime + m_audioStartDelayInSecond);
            }

            if (actionToRaiseOnEnd == null)
            {
                return;
            }

            OnFadeOutEnd.AddListener(actionToRaiseOnEnd);
        }

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

#if UNITY_EDITOR

        [SerializeField]
        private bool m_logDebug = false;

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void Editor_FadeIn()
        {
            FadeIn(1);
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void Editor_FadeOut()
        {
            FadeOut(1);
        }

#endif
    }
}