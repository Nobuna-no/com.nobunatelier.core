using UnityEngine;

namespace NobunAtelier
{
    public class CharacterControllerBase : MonoBehaviour
    {
        [SerializeField]
        protected Character m_controlledCharacter;

        public Character ControlledCharacter => m_controlledCharacter;

        [SerializeField]
        protected bool m_mountCharacterOnStart = true;

        protected virtual void Start()
        {
            if (!m_mountCharacterOnStart)
            {
                return;
            }

            m_controlledCharacter?.Mount(this);
        }

        protected virtual void UpdateController()
        { }

        protected virtual void OnEnable()
        { }

        protected virtual void OnDisable()
        { }
    }

    public abstract class CharacterControllerBase<T> : CharacterControllerBase
        where T : CharacterControllerModuleBase
    {
        [SerializeField]
        protected T[] m_modules;
        [SerializeField]
        private bool m_autoRefreshModule = true;

        public abstract bool IsAI { get; }

        public bool TryGetModule<ModuleType>(out ModuleType outModule) where ModuleType : CharacterControllerModuleBase
        {
            outModule = null;
            for (int i = 0, c = m_modules.Length; i < c; ++i)
            {
                var module = m_modules[i];
                if (module.GetType() == typeof(ModuleType))
                {
                    outModule = module as ModuleType;
                    return true;
                }
            }

            return false;
        }

        protected virtual void Awake()
        {
            if (!m_controlledCharacter)
            {
                Debug.LogWarning($"No Character set on {this}, searching for one in the parent hierarchy...");
                m_controlledCharacter = transform.parent.GetComponentInChildren<Character>();
                Debug.Assert(m_controlledCharacter, $"No Character found for {this.transform.parent.gameObject.name}/{this}");
            }

            if (m_autoRefreshModule)
            {
                CaptureCharacterControllerModules();
            }

            foreach (var extension in m_modules)
            {
                extension.InitModule(this);
            }
        }

        protected override void UpdateController()
        {
            if (m_controlledCharacter == null)
            {
                return;
            }

            foreach (var module in m_modules)
            {
                if (!module.IsAvailable())
                {
                    continue;
                }

                module.UpdateModule(Time.deltaTime);
            }
        }

        protected override void OnEnable()
        {
            foreach (var module in m_modules)
            {
                module.enabled = true;
            }
        }

        protected override void OnDisable()
        {
            foreach (var module in m_modules)
            {
                module.enabled = false;
            }
        }


        protected virtual void OnValidate()
        {
            if (m_autoRefreshModule)
            {
                CaptureCharacterControllerModules();
            }
        }

        [NaughtyAttributes.Button("Refresh modules")]
        protected virtual void CaptureCharacterControllerModules()
        {
            m_modules = GetComponents<T>();
        }
    }
}