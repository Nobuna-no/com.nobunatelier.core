using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Fade Only")]
    public class StateModule_FadeOnly : StateComponentModule
    {
        public enum StateChangeTrigger
        {
            None,
            OnFadeInEnd,
            OnFadeOutStart,
            OnFadeOutEnd
        }

        public enum ActivationTrigger
        {
            Manual,
            OnStateEnter,
            OnStateExit
        }

        [Header("State")]
        [SerializeField]
        private ActivationTrigger m_fadeInTrigger = ActivationTrigger.OnStateEnter;

        [SerializeField]
        private StateChangeTrigger m_stateChangeTrigger = StateChangeTrigger.None;

        [SerializeField, ShowIf("IsStateChangeTriggerActive")]
        private StateDefinition m_nextStateAfterFadeIn;

        [Header("Fade Events")]
        [SerializeField]
        private float m_delayBetweenFadeInAndOutInSecond = 0.5f;

        [SerializeField]
        private ScreenFader.FadingMode m_fadingInMode = ScreenFader.FadingMode.Normal;

        [SerializeField, ShowIf("IsNormalFadeIn")]
        private float m_fadeInDurationInSecond = 1.0f;

        public UnityEvent OnFadeInDone;

        [SerializeField]
        private ScreenFader.FadingMode m_fadingOutMode = ScreenFader.FadingMode.Normal;

        [SerializeField, ShowIf("IsNormalFadeOut")]
        private float m_fadeOutDurationInSecond = 1.0f;

        public UnityEvent OnFadeOutDone;

        private bool IsNormalFadeIn => m_fadingInMode == ScreenFader.FadingMode.Normal;
        private bool IsNormalFadeOut => m_fadingOutMode == ScreenFader.FadingMode.Normal;

        private bool IsStateChangeTriggerActive => m_stateChangeTrigger != StateChangeTrigger.None;

        public override void Enter()
        {
            if (m_fadeInTrigger == ActivationTrigger.OnStateEnter)
            {
                StartFading();
            }
        }

        public override void Exit()
        {
            if (m_fadeInTrigger == ActivationTrigger.OnStateExit)
            {
                StartFading();
            }
        }

        public void StartFading()
        {
            if (!ScreenFader.Instance)
            {
                FadeInEnd();
                return;
            }

            switch (m_fadingInMode)
            {
                case ScreenFader.FadingMode.Normal:
                    // ScreenFader.SetFaderDuration(m_fadeInDurationInSecond);
                    ScreenFader.FadeIn(m_fadeInDurationInSecond, FadeInEnd);
                    break;

                case ScreenFader.FadingMode.Instant:
                    ScreenFader.Fill();
                    FadeInEnd();
                    break;

                default:
                    FadeInEnd();
                    break;
            }
        }

        private void StartFadeOut()
        {
            switch (m_fadingOutMode)
            {
                case ScreenFader.FadingMode.Normal:
                    // ScreenFader.SetFaderDuration(m_fadeOutDurationInSecond);
                    ScreenFader.FadeOut(m_fadeOutDurationInSecond, FadeOutEnd);
                    break;

                case ScreenFader.FadingMode.Instant:
                    ScreenFader.Clear();
                    FadeOutEnd();
                    break;

                default:
                    FadeOutEnd();
                    break;
            }

            if (m_stateChangeTrigger == StateChangeTrigger.OnFadeOutStart && m_nextStateAfterFadeIn != null)
            {
                ModuleOwner.SetState(m_nextStateAfterFadeIn.GetType(), m_nextStateAfterFadeIn);
            }
        }

        private void FadeInEnd()
        {
            OnFadeInDone?.Invoke();

            if (m_stateChangeTrigger == StateChangeTrigger.OnFadeInEnd && m_nextStateAfterFadeIn != null)
            {
                ModuleOwner.SetState(m_nextStateAfterFadeIn.GetType(), m_nextStateAfterFadeIn);
            }

            if (m_delayBetweenFadeInAndOutInSecond > 0)
            {
                StartCoroutine(FadeMinimumDelay_Coroutine());
            }
            else
            {
                StartFadeOut();
            }
        }

        private void FadeOutEnd()
        {
            OnFadeOutDone?.Invoke();

            if (m_stateChangeTrigger == StateChangeTrigger.OnFadeOutEnd && m_nextStateAfterFadeIn != null)
            {
                ModuleOwner.SetState(m_nextStateAfterFadeIn.GetType(), m_nextStateAfterFadeIn);
            }
        }

        private IEnumerator FadeMinimumDelay_Coroutine()
        {
            yield return new WaitForSeconds(m_delayBetweenFadeInAndOutInSecond);

            StartFadeOut();
        }

        [System.Serializable]
        private struct SceneLoadingDescriptor
        {
            [NaughtyAttributes.Scene]
            public string[] Scenes;

            public UnityEvent OnScenesWorkDone;
        }
    }
}