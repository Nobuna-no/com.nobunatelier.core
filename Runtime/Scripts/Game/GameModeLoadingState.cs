using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class GameModeLoadingState : GameModeState
    {
        [Header("Fading")]
        [SerializeField]
        private GameModeStateDefinition m_nextStateAfterFade;

        [SerializeField]
        private bool m_useAnimatorFadeIn = true;

        [SerializeField, ShowIf("m_useAnimatorFadeIn")]
        private float m_delayBeforeFadeIn = 0f;
        private float m_remainingDelay = 0f;

        [ShowIf("m_useAnimatorFadeIn")]
        public UnityEvent OnFadeInDone;

        [SerializeField]
        private bool m_useAnimatorFadeOut = true;

        [ShowIf("m_useAnimatorFadeOut")]
        public UnityEvent OnFadeOutDone;

        public override void Enter()
        {
            base.Enter();

            if (m_delayBeforeFadeIn > 0)
            {
                this.enabled = true;
                m_remainingDelay = m_delayBeforeFadeIn;
            }
            else
            {
                this.enabled = false;
                FadeInStart();
            }
        }

        private void FadeInStart()
        {
            if (!m_useAnimatorFadeIn || !AnimatorFader.Instance)
            {
                FadeInEnd();
                return;
            }

            AnimatorFader.Instance.FadeIn(FadeInEnd);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            m_delayBeforeFadeIn -= deltaTime;
            if (m_delayBeforeFadeIn <= 0)
            {
                FadeInStart();
                this.enabled = false;
            }
        }

        private void FadeInEnd()
        {
            OnFadeInDone?.Invoke();

            if (!m_useAnimatorFadeOut || !AnimatorFader.Instance)
            {
                FadeOutEnd();
            }
            else
            {
                AnimatorFader.Instance.FadeOut(FadeOutEnd);
            }

            if (m_nextStateAfterFade == null)
            {
                return;
            }

            SetState(m_nextStateAfterFade);
        }

        private void FadeOutEnd()
        {
            OnFadeOutDone?.Invoke();
        }
    }
}