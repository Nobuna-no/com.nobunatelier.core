using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

#if UNITY_EDITOR

using UnityEditor.SearchService;
using UnityEditor.SceneManagement;

#endif

namespace NobunAtelier
{
    /// <summary>
    /// Provides methods to asynchronously and simultaneously load and unload scenes, 
    /// wrapping Unity async loading API.
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        [Header("Level Manager")]
        [SerializeField, Scene]
        private string m_persistentScene;

        public StringEvent OnSceneLoaded;
        public StringEvent OnSceneUnloaded;

        private List<AsyncSceneLoadingData> m_loadingScenes = new List<AsyncSceneLoadingData>(3);

        public void LoadSingleScene(string sceneName)
        {
            m_loadingScenes.Add(new AsyncSceneLoadingData(sceneName, LoadSceneMode.Single));
            this.enabled = true;
        }

        public void LoadSceneAdditive(string sceneName)
        {
            m_loadingScenes.Add(new AsyncSceneLoadingData(sceneName, LoadSceneMode.Additive));
            this.enabled = true;
        }

        public void UnloadScene(string sceneName)
        {
            m_loadingScenes.Add(new AsyncSceneLoadingData(sceneName));
            this.enabled = true;
        }

        protected override void OnSingletonAwake()
        {
            this.enabled = false;
        }

        private void Update()
        {
            for (int i = m_loadingScenes.Count - 1; i >= 0; --i)
            {
                var scene = m_loadingScenes[i];
                if (scene.IsWorkDone())
                {
                    if (scene.IsLoading)
                    {
                        OnSceneLoaded?.Invoke(scene.SceneName);
                        Debug.Log($"Scene '{scene.SceneName}' Loaded");
                    }
                    else
                    {
                        OnSceneUnloaded?.Invoke(scene.SceneName);
                        Debug.Log($"Scene '{scene.SceneName}' Unloaded");
                    }
                    m_loadingScenes.RemoveAt(i);
                }
            }

            if (m_loadingScenes.Count == 0)
            {
                this.enabled = false;
            }
        }

#if UNITY_EDITOR

        private void Start()
        {
            PlaymodeRemoveScenesButPersistent();
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void PlaymodeRemoveScenesButPersistent()
        {
            if (m_persistentScene.Length == 0)
            {
                return;
            }

            for (int i = SceneManager.sceneCount - 1; i >= 0; --i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name != m_persistentScene)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    SceneManager.UnloadScene(scene);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        [Button(enabledMode: EButtonEnableMode.Editor)]
        private void EditorSaveAndUnloadScenesButPersistentInHierarchy()
        {
            if (m_persistentScene.Length == 0)
            {
                return;
            }

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            foreach (var s in setup)
            {
                if (!s.path.Contains(m_persistentScene) && s.isLoaded)
                {
                    var scene = EditorSceneManager.GetSceneByPath(s.path);
                    EditorSceneManager.SaveScene(scene);
                    EditorSceneManager.CloseScene(scene, false);
                }
            }
        }

        [Button(enabledMode: EButtonEnableMode.Editor)]
        private void EditorLoadScenesInHierarchy()
        {
            if (m_persistentScene.Length == 0)
            {
                return;
            }

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            foreach (var s in setup)
            {
                if (!s.path.Contains(m_persistentScene) && !s.isActive && !s.isLoaded)
                {
                    EditorSceneManager.OpenScene(s.path, OpenSceneMode.Additive);
                }
            }
        }

#endif

        private class AsyncSceneLoadingData
        {
            public string SceneName { get; private set; }
            public bool IsLoading { get; private set; }
            private AsyncOperation m_asyncOp;
            private bool m_resourcesReleased = false;

            public AsyncSceneLoadingData(string sceneName, LoadSceneMode loadSceneMode)
            {
                SceneName = sceneName;
                m_asyncOp = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                IsLoading = true;
            }

            public AsyncSceneLoadingData(string sceneName)
            {
                SceneName = sceneName;
                m_asyncOp = SceneManager.UnloadSceneAsync(sceneName);
                IsLoading = false;
            }

            public bool IsWorkDone()
            {
                if (!m_asyncOp.isDone)
                {
                    return false;
                }

                if (m_resourcesReleased)
                {
                    m_resourcesReleased = false;
                    return true;
                }

                m_resourcesReleased = true;
                m_asyncOp = Resources.UnloadUnusedAssets();
                return false;
            }
        }
    }
}