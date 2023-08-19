using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterControllerBase : MonoBehaviour
    {
        public enum EnableInputBehaviour
        {
            OnAwake,
            OnEnable,
            Manual
        }

        public enum DisableInputBehaviour
        {
            OnDisable,
            Manual
        }

        [Header("Controller")]
        [SerializeField]
        protected Character m_controlledCharacter;

        public Character ControlledCharacter => m_controlledCharacter;

        [SerializeField]
        protected bool m_mountCharacterOnStart = true;

        [SerializeField]
        private EnableInputBehaviour m_enableInputBehaviour = EnableInputBehaviour.OnAwake;

        [SerializeField, Tooltip("DisableInput is always called OnDestroy.")]
        private DisableInputBehaviour m_disableInputBehaviour = DisableInputBehaviour.Manual;

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public abstract void EnableInput();

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public abstract void DisableInput();

        protected virtual void Awake()
        {
            if (m_enableInputBehaviour == EnableInputBehaviour.OnAwake)
            {
                EnableInput();
            }
        }

        protected virtual void Start()
        {
            if (!m_mountCharacterOnStart)
            {
                return;
            }

            m_controlledCharacter?.Mount(this);
        }

        protected virtual void UpdateController(float deltaTime)
        { }

        protected virtual void OnEnable()
        {
            if (m_enableInputBehaviour == EnableInputBehaviour.OnEnable)
            {
                EnableInput();
            }
        }

        protected virtual void OnDisable()
        {
            if (m_disableInputBehaviour == DisableInputBehaviour.OnDisable)
            {
                DisableInput();
            }
        }
    }

    public abstract class CharacterControllerBase<T> : CharacterControllerBase
        where T : CharacterControllerModuleBase
    {
        [Header("Modules")]
        [SerializeField]
        protected T[] m_modules;

        [SerializeField]
        private bool m_autoCaptureModule = true;

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

        protected override void Awake()
        {
            if (!m_controlledCharacter)
            {
                Debug.LogWarning($"No Character set on {this}, searching for one in the parent hierarchy...");
                m_controlledCharacter = transform.parent.GetComponentInChildren<Character>();
                Debug.Assert(m_controlledCharacter, $"No Character found for {this.transform.parent.gameObject.name}/{this}");
            }

            if (m_autoCaptureModule)
            {
                CaptureCharacterControllerModules();
            }

            foreach (var module in m_modules)
            {
                module.InitModule(this);
            }

            base.Awake();
        }

        protected virtual void OnDestroy()
        {
            DisableInput();
        }

        protected override void UpdateController(float deltaTime)
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

                module.UpdateModule(deltaTime);
            }
        }

        protected override void OnEnable()
        {
            foreach (var module in m_modules)
            {
                module.enabled = true;
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            foreach (var module in m_modules)
            {
                module.enabled = false;
            }

            base.OnDisable();
        }

        protected virtual void OnValidate()
        {
            if (m_autoCaptureModule)
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