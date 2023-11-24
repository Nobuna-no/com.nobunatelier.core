using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Utils/Screen Fader: CrossFade")]
    [RequireComponent(typeof(Animator), typeof(Image))]
    public class ScreenFader_CrossFade : ScreenFader
    {
        [Header("Screen Fader: CrossFade")]
        [SerializeField] private string m_fillStateName;

        [SerializeField] private string m_clearStateName;

        private Animator m_animator;
        private float m_currentTime = 0f;
        private int m_fillStateHash;
        private int m_clearStateHash;
        private bool m_isCrossFadingIn = false;
        private Image m_image;
#if UNITY_EDITOR

        [Header("Cross Fade Debug")]
        [SerializeField]
        private float m_debugCrossFadeDuration = 1f;

#endif

        protected override void OnSingletonAwake()
        {
            m_image = GetComponent<Image>();
            m_image.enabled = false;
            m_animator = GetComponent<Animator>();
            m_fillStateHash = Animator.StringToHash(m_fillStateName);
            m_clearStateHash = Animator.StringToHash(m_clearStateName);

            Debug.Assert(m_animator.HasState(0, m_fillStateHash), $"{this.GetType().Name}: Cannot find valid Fill state name '{m_fillStateName}' in AnimatorController.");
            Debug.Assert(m_animator.HasState(0, m_clearStateHash), $"{this.GetType().Name}: Cannot find valid Fill state name '{m_clearStateName}' in AnimatorController.");
        }

        protected override void FillImpl()
        {
            base.FillImpl();

            if (!m_animator)
            {
                return;
            }

            m_animator.CrossFadeInFixedTime(m_fillStateName, 0, 0);

            m_isCrossFadingIn = true;
            m_image.enabled = true;
        }

        // Instantly fill the screen
        protected override void ClearImpl()
        {
            base.ClearImpl();

            if (!m_animator)
            {
                return;
            }

            m_animator.CrossFadeInFixedTime(m_clearStateName, 0, 0);

            m_isFadeIn = false;
            m_image.enabled = false;
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void Editor_CrossFadeIn()
        {
            FadeIn(m_debugCrossFadeDuration, null);
        }

#endif

        protected override void FadeInImpl(float duration, UnityAction actionToRaiseOnEnd = null)
        {
            if (!m_animator)
            {
                return;
            }

            m_image.enabled = true;

            if (m_currentTime != -1)
            {
                if (m_isCrossFadingIn)
                {
                    // force finish previous fade in and start new one.
                    FadeInEnd();
                    m_isFadeIn = false;
                }
                else
                {
                    FadeOutEnd();
                }
            }

            if (m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            m_animator.CrossFadeInFixedTime(m_fillStateHash, duration, 0);
            m_currentTime = duration;
            m_isCrossFadingIn = true;

            base.FadeInImpl(duration, actionToRaiseOnEnd);
        }

#if UNITY_EDITOR

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void Editor_CrossFadeOut()
        {
            FadeOut(m_debugCrossFadeDuration, null);
        }

#endif

        protected override void FadeOutImpl(float duration, UnityAction actionToRaiseOnEnd = null)
        {
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
                    m_isFadeIn = true;
                }
                else
                {
                    FadeInEnd();
                }
            }

            if (!m_isFadeIn)
            {
                actionToRaiseOnEnd?.Invoke();
                return;
            }

            m_animator.CrossFadeInFixedTime(m_clearStateHash, duration, 0);
            m_currentTime = duration;
            m_isCrossFadingIn = false;

            base.FadeOutImpl(duration, actionToRaiseOnEnd);
        }

        protected override void FadeOutEnd()
        {
            base.FadeOutEnd();
            m_image.enabled = false;
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
    }
}