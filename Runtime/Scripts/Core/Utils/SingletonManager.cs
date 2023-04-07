using UnityEngine;

public abstract class SingletonManager<T> : MonoBehaviour
    where T: MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Constructor() { }
    protected abstract T GetInstance();

    [Header("Singleton")]
    [SerializeField]
    private bool m_destroyGameObjectIfAlreadyExist = true;
    [SerializeField]
    private bool m_dontDestroyOnLoad = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        if (Instance != null)
        {
            Debug.Log($"Instance '{Instance.name}' reset on SubsystemRegistration");
            Instance = null;
        }
    }

    protected virtual void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Debug.LogWarning($"Singleton Manager of type <{this.GetType().Name}> already exist. Destroying {this}...");
            Destroy(m_destroyGameObjectIfAlreadyExist ? gameObject : this);
        }

        Instance = GetInstance();

        if (m_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(Instance);
        }
    }
}




