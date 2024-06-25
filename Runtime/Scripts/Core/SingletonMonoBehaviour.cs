using System.Collections;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Improved version of the Singleton pattern for Unity.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviourManager
        where T : MonoBehaviourManager
    {
        [Header("Singleton")]
        [SerializeField] private bool m_dontDestroyOnLoad = false;

        [Tooltip("If a singleton already exist, any a new singleton instance is going to be destroyed." +
            "Set this value to true to only destroy the component instead of the entire GameObject.")]
        [SerializeField] private bool m_onlyDestroyDuplicatedComponent = true;

        public static T Instance { get; private set; }
        public static bool IsSingletonValid => Instance != null;

        public static void CreateAndInitialize()
        {
            if (Instance == null)
            {
                var singleton = new GameObject($"[ {typeof(T).Name} ]").AddComponent<T>();
            }
            if (IsSingletonValid == false)
            {
                Debug.LogError("Singleton failed to call awake?");
            }
            Instance.Initialize();
        }

        /// <summary>
        /// Can be call in a bootstrapper to initialize all managers.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator CreateAndInitializeRoutine()
        {
            if (Instance == null)
            {
                var singleton = new GameObject($"[ {typeof(T).Name} ]").GetComponent<T>();
            }
            if (IsSingletonValid == false)
            {
                Debug.LogError("Singleton failed to call awake?");
            }

            yield return Instance.Initialize();
        }

        internal override sealed IEnumerator Initialize()
        {
            yield return SingletonInitialization();
        }

        internal override sealed void Terminate()
        {
            OnSingletonTermination();
        }

        protected virtual IEnumerator SingletonInitialization()
        {
            yield break;
        }

        protected virtual void OnSingletonTermination()
        { }

        protected virtual void OnSingletonAwake()
        { }

        protected virtual void OnSingletonApplicationQuit()
        { }

#if UNITY_EDITOR
        /// <summary>
        /// Mandatory for quick enter playmode.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistrationInstanceReset()
        {
            Instance = null;
        }

#endif

        protected override sealed void Awake()
        {
            if (!TryInitializeInstance(this))
            {
                return;
            }

            if (m_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(Instance);
            }

            OnSingletonAwake();
        }

        // Inspiration: Iain McManus - Unity Design Patterns: Singleton Deep Dive
        private bool TryInitializeInstance(SingletonMonoBehaviour<T> newInstance)
        {
            if (Instance != this && Instance != null)
            {
                Debug.LogWarning($"Singleton of type <{this.GetType().Name}> already exist. Destroying {this}...");
                Destroy(m_onlyDestroyDuplicatedComponent ? this : gameObject);
                return false;
            }

            Instance = newInstance as T;
            return true;
        }

        private void OnApplicationQuit()
        {
            OnSingletonApplicationQuit();
            Instance = null;
        }
    }
}