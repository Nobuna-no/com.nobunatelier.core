using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Fade And Scene Loading")]
    public class StateModule_FadeAndSceneLoading : StateComponentModule
    {
        public enum Trigger
        {
            Manual,
            OnStateEnter,
            OnStateExit
        }

        [Header("State")]
        [SerializeField]
        private Trigger m_trigger = Trigger.OnStateEnter;

        [SerializeField]
        private StateDefinition m_nextStateAfterScenesWork;

        [Header("Fade Events")]
        [SerializeField]
        private float m_delayBetweenFadeInAndOutInSecond = 0.5f;

        [SerializeField]
        private FadingMode m_fadingInMode = FadingMode.Normal;

        [SerializeField, ShowIf("IsNormalFadeIn")]
        private float m_fadeInDurationInSecond = 1.0f;

        // Is it really useful?
        // public UnityEvent OnFadeInDone;

        [SerializeField]
        private FadingMode m_fadingOutMode = FadingMode.Normal;

        [SerializeField, ShowIf("IsNormalFadeOut")]
        private float m_fadeOutDurationInSecond = 1.0f;

        // Is it really useful?
        // public UnityEvent OnFadeOutDone;

        [Header("Scenes Loading")]
        [InfoBox("On scene loading: Fade In then Fade Out\nOn scene unloading: Fade In only")]
        // Could be improved with the use of a BlackBoard - Get the scene to load from the blackboard...
        [NaughtyAttributes.Scene]
        public string[] m_scenesToLoad;

        [NaughtyAttributes.Scene]
        public string[] m_scenesToUnload;

        public UnityEvent OnScenesWorkDone;

        private List<string> m_loadingScenes = new List<string>();
        private List<string> m_unloadingScenes = new List<string>();

        private bool IsNormalFadeIn => m_fadingInMode == FadingMode.Normal;
        private bool IsNormalFadeOut => m_fadingOutMode == FadingMode.Normal;

        public override void Enter()
        {
            if (m_trigger == Trigger.OnStateEnter)
            {
                StartFading();
            }
        }

        public override void Exit()
        {
            if (m_trigger == Trigger.OnStateExit)
            {
                StartFading();
            }
        }

        public void StartFading()
        {
            if (!AnimatorFader.Instance)
            {
                FadeInEnd();
                return;
            }

            switch (m_fadingInMode)
            {
                case FadingMode.Normal:
                    AnimatorFader.Instance.SetFaderDuration(m_fadeInDurationInSecond);
                    AnimatorFader.Instance.FadeIn(FadeInEnd);
                    break;

                case FadingMode.Instant:
                    AnimatorFader.Instance.Fill();
                    FadeInEnd();
                    break;

                default:
                    FadeInEnd();
                    break;
            }
        }

        private bool LoadScenes()
        {
            m_loadingScenes.Clear();
            if (m_scenesToLoad.Length == 0)
            {
                return false;
            }

            if (!LevelManager.Instance)
            {
                Debug.LogWarning("Trying to load a level but no LevelManager Instance found!");
                return false;
            }

            m_loadingScenes.AddRange(m_scenesToLoad);

            LevelManager.Instance.OnSceneLoaded.AddListener(OnSceneLoaded);
            foreach (var scene in m_scenesToLoad)
            {
                LevelManager.Instance.LoadSceneAdditive(scene);
            }

            return true;
        }

        private bool UnloadScenes()
        {
            m_unloadingScenes.Clear();
            if (m_scenesToUnload.Length == 0)
            {
                return false;
            }

            if (!LevelManager.Instance)
            {
                Debug.LogWarning("Trying to load a level but no LevelManager Instance found!");
                return false;
            }

            m_unloadingScenes.AddRange(m_scenesToUnload);

            LevelManager.Instance.OnSceneUnloaded.AddListener(OnSceneLoaded);
            foreach (var scene in m_scenesToUnload)
            {
                LevelManager.Instance.UnloadScene(scene);
            }

            return true;
        }

        private void OnSceneLoaded(string sceneName)
        {
            bool hasLoadedScene = m_loadingScenes.Contains(sceneName);
            bool hasUnloadedScene = m_unloadingScenes.Contains(sceneName);

            if (!hasLoadedScene && !hasUnloadedScene)
            {
                return;
            }

            if (hasLoadedScene)
            {
                LevelManager.Instance?.OnSceneLoaded.RemoveListener(OnSceneLoaded);
                m_loadingScenes.Remove(sceneName);
            }
            if (hasUnloadedScene)
            {
                LevelManager.Instance?.OnSceneUnloaded.RemoveListener(OnSceneLoaded);
                m_unloadingScenes.Remove(sceneName);
            }

            if (IsSceneWorkDone())
            {
                OnAllScenesLoaded();
            }
        }

        private bool IsSceneWorkDone()
        {
            return m_loadingScenes.Count == 0 && m_unloadingScenes.Count == 0;
        }

        private void OnAllScenesLoaded()
        {
            OnScenesWorkDone?.Invoke();

            switch (m_fadingOutMode)
            {
                case FadingMode.Normal:
                    AnimatorFader.Instance.SetFaderDuration(m_fadeOutDurationInSecond);
                    AnimatorFader.Instance.FadeOut(FadeOutEnd);
                    break;

                case FadingMode.Instant:
                    AnimatorFader.Instance.Clear();
                    FadeOutEnd();
                    break;

                default:
                    FadeOutEnd();
                    break;
            }

            if (m_nextStateAfterScenesWork != null)
            {
                ModuleOwner.SetState(m_nextStateAfterScenesWork.GetType(), m_nextStateAfterScenesWork);
            }
        }

        private void FadeInEnd()
        {
            AnimatorFader.Instance.ResetFaderDuration();
            // OnFadeInDone?.Invoke();

            bool isDoingSceneWork = LoadScenes();
            isDoingSceneWork |= UnloadScenes();

            if (isDoingSceneWork)
            {
                return;
            }

            if (m_delayBetweenFadeInAndOutInSecond > 0)
            {
                StartCoroutine(FadeMinimumDelay_Coroutine());
            }
            else if (IsSceneWorkDone())
            {
                OnAllScenesLoaded();
            }
        }

        private void FadeOutEnd()
        {
            AnimatorFader.Instance.ResetFaderDuration();
            // OnFadeOutDone?.Invoke();
        }

        private IEnumerator FadeMinimumDelay_Coroutine()
        {
            yield return new WaitForSeconds(m_delayBetweenFadeInAndOutInSecond);

            if (IsSceneWorkDone())
            {
                OnAllScenesLoaded();
            }
        }
    }
}