using UnityEngine;

// Improved version of the Singleton pattern for Unity.
public abstract class Singleton<T> : MonoBehaviour
    where T : MonoBehaviour
{
    [SerializeField] private bool m_dontDestroyOnLoad = false;

    [Header("Singleton")]
    [Tooltip("If a singleton already exist, any a new singleton instance is going to be destroyed." +
        "Set this value to true to only destroy the component instead of the entire GameObject.")]
    [SerializeField] private bool m_onlyDestroyDuplicatedComponent = true;

    public static T Instance { get; private set; }
    public static bool IsSingletonValid => Instance != null;

    protected virtual void OnSingletonAwake()
    { }

    protected virtual void OnSingletonApplicationQuit()
    { }

#if UNITY_EDITOR

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void SubsystemRegistrationInstanceReset()
    {
        Instance = null;
    }

#endif

    private void Awake()
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
    private bool TryInitializeInstance(Singleton<T> newInstance)
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