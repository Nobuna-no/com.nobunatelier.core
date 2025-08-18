using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public abstract class CharacterControllerBase : MonoBehaviour
    {
        public enum EnableInputBehaviour
        {
            OnAwake,
            OnEnable,
            OnStart,
            Manual
        }

        public enum DisableInputBehaviour
        {
            OnDisable,
            Manual
        }

        [Header("Controller")]
        [SerializeField]
        [FormerlySerializedAs("m_controlledCharacter")]
        protected Character m_ControlledCharacter;

        public Character ControlledCharacter => m_ControlledCharacter;

        // [SerializeField]
        // protected bool m_mountCharacterOnStart = true;

        [SerializeField]
        [FormerlySerializedAs("m_enableInputBehaviour")]
        private EnableInputBehaviour m_EnableInputBehaviour = EnableInputBehaviour.OnAwake;

        [SerializeField, Tooltip("DisableInput is always called OnDestroy.")]
        [FormerlySerializedAs("m_disableInputBehaviour")]
        private DisableInputBehaviour m_DisableInputBehaviour = DisableInputBehaviour.Manual;

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public abstract void EnableInput();

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public abstract void DisableInput();

        public virtual void SetCharacterReference(Character character)
        {
            m_ControlledCharacter = character;
        }

        protected virtual void Awake()
        {
            if (m_EnableInputBehaviour == EnableInputBehaviour.OnAwake)
            {
                EnableInput();
            }
        }

        protected virtual void Start()
        {
            if (m_EnableInputBehaviour == EnableInputBehaviour.OnStart)
            {
                EnableInput();
            }

            m_ControlledCharacter?.SetController(this);
        }

        protected virtual void UpdateController(float deltaTime)
        { }

        protected virtual void OnEnable()
        {
            if (m_EnableInputBehaviour == EnableInputBehaviour.OnEnable)
            {
                EnableInput();
            }
        }

        protected virtual void OnDisable()
        {
            if (m_DisableInputBehaviour == DisableInputBehaviour.OnDisable)
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
        [FormerlySerializedAs("m_modules")]
        protected T[] m_Modules;

        [FormerlySerializedAs("m_autoCaptureModule")]
        [SerializeField] private bool m_AutoCaptureModule = true;

        [SerializeField, Tooltip("Is the controller use in standalone without a character?")]
        [FormerlySerializedAs("m_isStandalone")]
        private bool m_IsStandalone = false;

        public abstract bool IsAI { get; }
        public bool IsStandalone => m_IsStandalone;

        public bool TryGetModule<ModuleType>(out ModuleType outModule) where ModuleType : CharacterControllerModuleBase
        {
            outModule = null;
            for (int i = 0, c = m_Modules.Length; i < c; ++i)
            {
                var module = m_Modules[i];
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
            if (!m_ControlledCharacter && !m_IsStandalone)
            {
                Debug.LogWarning($"No Character set on {this}, searching for one in the parent hierarchy...");
                m_ControlledCharacter = transform.parent.GetComponentInChildren<Character>();
                Debug.Assert(m_ControlledCharacter, $"No Character found for {this.transform.parent.gameObject.name}/{this}");
            }

            if (m_AutoCaptureModule)
            {
                CaptureCharacterControllerModules();
            }

            foreach (var module in m_Modules)
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
            if (!m_IsStandalone && m_ControlledCharacter == null)
            {
                return;
            }

            foreach (var module in m_Modules)
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
            foreach (var module in m_Modules)
            {
                module.enabled = true;
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            foreach (var module in m_Modules)
            {
                module.enabled = false;
            }

            base.OnDisable();
        }

        protected virtual void OnValidate()
        {
            if (m_AutoCaptureModule)
            {
                CaptureCharacterControllerModules();
            }
        }

        [NaughtyAttributes.Button("Refresh modules")]
        protected virtual void CaptureCharacterControllerModules()
        {
            m_Modules = GetComponents<T>();
        }
    }
}