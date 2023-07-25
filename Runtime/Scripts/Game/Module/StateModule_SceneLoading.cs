using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class StateModule_SceneLoading : StateModuleBase
    {
        [Header("State")]
        [SerializeField]
        private StateDefinition m_nextStateAfterScenesWork;

        [Header("Scenes Loading")]
        [InfoBox("On scene loading: Fade In then Fade Out\nOn scene unloading: Fade In only")]
        // Could be improved with the use of a BlackBoard - Get the scene to load from the blackboard...
        [SerializeField]
        private SceneLoadingDescriptor m_toLoad;

        [SerializeField]
        private SceneLoadingDescriptor m_toUnload;

        [Header("Fade Events")]
        [SerializeField]
        private FadingMode m_fadingInMode = FadingMode.Normal;

        public UnityEvent OnFadeInDone;

        [SerializeField]
        private FadingMode m_fadingOutMode = FadingMode.Normal;

        public UnityEvent OnFadeOutDone;

        private List<string> m_loadingScenes = new List<string>();
        private List<string> m_unloadingScenes = new List<string>();

        public override void Enter()
        {
            base.Enter();

            if (!AnimatorFader.Instance)
            {
                // Debug.Log("No AnimatorFader found - no fade in animation played - invoking OnFadeInDone now.");
                FadeInEnd();
                return;
            }

            switch (m_fadingInMode)
            {
                case FadingMode.Normal:
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

        private void LoadScenes()
        {
            m_loadingScenes.Clear();
            if (m_toLoad.Scenes.Length == 0)
            {
                return;
            }

            if (!LevelManager.Instance)
            {
                Debug.LogWarning("Trying to load a level but no LevelManager Instance found!");
                return;
            }

            m_loadingScenes.AddRange(m_toLoad.Scenes);

            LevelManager.Instance.OnSceneLoaded.AddListener(OnSceneLoaded);
            foreach (var scene in m_toLoad.Scenes)
            {
                LevelManager.Instance.LoadSceneAdditive(scene);
            }
        }

        private void UnloadScenes()
        {
            m_unloadingScenes.Clear();
            if (m_toUnload.Scenes.Length == 0)
            {
                return;
            }

            if (!LevelManager.Instance)
            {
                Debug.LogWarning("Trying to load a level but no LevelManager Instance found!");
                return;
            }

            m_unloadingScenes.AddRange(m_toUnload.Scenes);

            LevelManager.Instance.OnSceneUnloaded.AddListener(OnSceneLoaded);
            foreach (var scene in m_toUnload.Scenes)
            {
                LevelManager.Instance.UnloadScene(scene);
            }
        }

        private void OnSceneLoaded(string sceneName)
        {
            bool isLoadedScene = m_loadingScenes.Contains(sceneName);
            bool isUnloadedScene = m_unloadingScenes.Contains(sceneName);

            if (!isLoadedScene && !isUnloadedScene)
            {
                return;
            }

            if (isLoadedScene)
            {
                LevelManager.Instance?.OnSceneLoaded.RemoveListener(OnSceneLoaded);
                m_loadingScenes.Remove(sceneName);
            }
            if (isUnloadedScene)
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
            if (m_toLoad.Scenes.Length > 0)
            {
                m_toLoad.OnScenesWorkDone?.Invoke();
            }
            if (m_toUnload.Scenes.Length > 0)
            {
                m_toUnload.OnScenesWorkDone?.Invoke();
            }

            switch (m_fadingOutMode)
            {
                case FadingMode.Normal:
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

            ModuleOwner.SetState(m_nextStateAfterScenesWork.GetType(), m_nextStateAfterScenesWork);
        }

        private void FadeInEnd()
        {
            OnFadeInDone?.Invoke();

            LoadScenes();
            UnloadScenes();

            if (IsSceneWorkDone())
            {
                OnAllScenesLoaded();
            }
        }

        private void FadeOutEnd()
        {
            OnFadeOutDone?.Invoke();
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