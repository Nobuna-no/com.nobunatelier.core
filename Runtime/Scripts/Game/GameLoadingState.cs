using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State: Loading")]
    public class GameLoadingState : BaseState<GameStateDefinition, GameStateCollection>
    {
        [Header("Loading")]
        [SerializeField]
        private GameStateDefinition m_nextStateAfterScenesWork;

        [InfoBox("On scene loading: Fade In then Fade Out\nOn scene unloading: Fade In only")]
        // Could be improved with the use of a BlackBoard - Get the scene to load from the blackboard...
        [SerializeField, NaughtyAttributes.Scene]
        private string[] m_scenesToLoad;

        [ShowIf("IsSceneLoaded")]
        public UnityEvent OnScenesLoaded;

        [SerializeField, NaughtyAttributes.Scene]
        private string[] m_scenesToUnload;

        [ShowIf("IsSceneUnloaded")]
        public UnityEvent OnScenesUnloaded;

        [Header("Fade Events")]
        [SerializeField]
        public bool m_useAnimatorInstantFill = false;

        [SerializeField]
        public bool m_useAnimatorFadeIn = true;

        [SerializeField]
        public bool m_useAnimatorFadeOut = true;

        [ShowIf("m_useAnimatorFadeIn")]
        public UnityEvent OnFadeInDone;

        [ShowIf("m_useAnimatorFadeOut")]
        public UnityEvent OnFadeOutDone;

        private List<string> m_loadingScenes = new List<string>();
        private List<string> m_unloadingScenes = new List<string>();

        private bool IsSceneUnloaded()
        {
            return m_scenesToUnload != null && m_scenesToUnload.Length > 0;
        }

        private bool IsSceneLoaded()
        {
            return m_scenesToLoad != null && m_scenesToLoad.Length > 0;
        }

        public override void Enter()
        {
            base.Enter();

            if (!ScreenFader.IsInstanceValid())
            {
                // Debug.Log("No AnimatorFader found - no fade in animation played - invoking OnFadeInDone now.");
                FadeInEnd();
                return;
            }

            if (m_useAnimatorFadeIn)
            {
                ScreenFader.FadeIn(FadeInEnd);
            }
            else
            {
                if (m_useAnimatorInstantFill)
                {
                    ScreenFader.Fill();
                }

                FadeInEnd();
            }
        }

        private void LoadScenes()
        {
            m_loadingScenes.Clear();
            if (m_scenesToLoad.Length == 0)
            {
                return;
            }

            if (!LevelManager.Instance)
            {
                Debug.LogWarning("Trying to load a level but no LevelManager Instance found!");
                return;
            }

            m_loadingScenes.AddRange(m_scenesToLoad);

            LevelManager.Instance.OnSceneLoaded.AddListener(OnSceneLoaded);
            foreach (var scene in m_scenesToLoad)
            {
                LevelManager.Instance.LoadSceneAdditive(scene);
            }
        }

        private void UnloadScenes()
        {
            m_unloadingScenes.Clear();
            if (m_scenesToUnload.Length == 0)
            {
                return;
            }

            if (!LevelManager.Instance)
            {
                Debug.LogWarning("Trying to load a level but no LevelManager Instance found!");
                return;
            }

            m_unloadingScenes.AddRange(m_scenesToUnload);

            LevelManager.Instance.OnSceneUnloaded.AddListener(OnSceneLoaded);
            foreach (var scene in m_scenesToUnload)
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
            if (m_scenesToLoad.Length > 0)
            {
                OnScenesLoaded?.Invoke();
            }
            if (m_scenesToUnload.Length > 0)
            {
                OnScenesUnloaded?.Invoke();
            }

            if (!m_useAnimatorFadeOut || !ScreenFader.IsInstanceValid())
            {
                FadeOutEnd();
            }
            else
            {
                ScreenFader.FadeOut(FadeOutEnd);
            }

            if (m_nextStateAfterScenesWork == null)
            {
                return;
            }

            SetState(m_nextStateAfterScenesWork);
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
    }
}