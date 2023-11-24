using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class GameModeLoadingState : GameModeState
    {
        [InfoBox("On scene loading: Fade In then Fade Out\nOn scene unloading: Fade In only")]
        // Could be improved with the use of a BlackBoard - Get the scene to load from the blackboard...
        [SerializeField, NaughtyAttributes.Scene]
        private string[] m_scenesToLoad;

        [SerializeField, NaughtyAttributes.Scene]
        private string[] m_scenesToUnload;

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
            if (!m_useAnimatorFadeIn || !ScreenFader.Instance)
            {
                FadeInEnd();
                return;
            }

            ScreenFader.FadeIn(FadeInEnd);
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

            if (!m_useAnimatorFadeOut || !ScreenFader.Instance)
            {
                FadeOutEnd();
            }
            else
            {
                ScreenFader.FadeOut(FadeOutEnd);
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