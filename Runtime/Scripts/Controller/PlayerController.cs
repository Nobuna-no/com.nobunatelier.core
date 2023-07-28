using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/PlayerController")]
    public class PlayerController : CharacterControllerBase<PlayerControllerModuleBase>
    {
        public override bool IsAI => false;
        public PlayerInput PlayerInput => m_playerInput;

        [SerializeField]
        protected PlayerInput m_playerInput;

        [SerializeField, Tooltip("Action map used by this controller to get bindings from.")]
        private string m_actionMapName = "Player";

        [SerializeField, ShowIf("IsPlayerInputValid")]
        private bool m_mountInputOnAwake = false;
        [SerializeField, ShowIf("IsPlayerInputValid")]
        private bool m_mountInputOnEnable = false;


        protected InputActionMap ActionMap => m_actionMap;
        protected string ActionMapName => m_actionMapName;
        protected InputActionMap ActiveActionMap => m_actionMap;
        public bool IsInputReady { get; private set; } = false;

        private InputActionMap m_actionMap;

        // [SerializeField]
        // protected PlayerControllerModuleBase[] m_modules;

        protected override void Awake()
        {
            base.Awake();

            CapturePlayerInputAndSetBehaviour();

            if (m_playerInput && m_mountInputOnAwake)
            {
                EnableInput();
            }
        }

        public virtual void MountPlayerInput(PlayerInput player, bool enableInput = true)
        {
            m_playerInput = player;

            if (enableInput)
            {
                EnableInput();
            }
        }

        public virtual void UnMountPlayerInput()
        {
            DisableInput();
            m_playerInput = null;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        /// <summary>
        /// Activate PlayerInput and switch action map to <cref="m_actionMapName">m_actionMapName</cref>
        /// Access the active action map using <cref="ActiveActionMap">ActiveActionMap</cref>.
        /// Don't forget to call the base.EnableInput() in order to initialize the player input!
        /// </summary>
        public virtual void EnableInput()
        {
            Debug.Assert(m_playerInput != null, $"[{Time.frameCount}] {this}: PlayerInput is required");

            m_playerInput.ActivateInput();
            m_playerInput.SwitchCurrentActionMap(m_actionMapName);
            m_actionMap = m_playerInput.actions.FindActionMap(m_actionMapName);

            IsInputReady = true;

            foreach (var extension in m_modules)
            {
                extension.EnableModuleInput(PlayerInput, ActiveActionMap);
            }
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void DisableInput()
        {
            m_playerInput.DeactivateInput();
            m_actionMap = null;
            IsInputReady = false;

            foreach (var extension in m_modules)
            {
                extension.DisableModuleInput(PlayerInput, ActiveActionMap);
            }
        }

        protected override void UpdateController()
        {
            if (m_controlledCharacter == null)
            {
                return;
            }

            foreach (var extension in m_modules)
            {
                if (!extension.IsAvailable())
                {
                    continue;
                }

                extension.UpdateModule(Time.deltaTime);
            }
        }

        private bool IsPlayerInputValid()
        {
            return m_playerInput != null;
        }

        protected virtual void OnDestroy()
        {
            UnMountPlayerInput();
        }

        protected override void OnEnable()
        {
            if (m_mountInputOnEnable)
            {
                EnableInput();
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (m_mountInputOnEnable)
            {
                DisableInput();
            }

            base.OnDisable();
        }

        private void Update()
        {
            UpdateController();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            CapturePlayerInputAndSetBehaviour();
        }

        private void CapturePlayerInputAndSetBehaviour()
        {
            if (m_playerInput == null)
            {
                m_playerInput = GetComponent<PlayerInput>();
            }

            if (m_playerInput)
            {
                m_playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            }
        }
    }

    //public class PlayerController : LegacyPlayerControllerBase
    //{
    //    [SerializeField]
    //    protected PlayerControllerModuleBase[] m_modules;

    //    protected override void Awake()
    //    {
    //        base.Awake();

    //        foreach(var extension in m_modules)
    //        {
    //            extension.InitModule(this);
    //        }
    //    }

    //    public override void EnableInput()
    //    {
    //        Debug.Assert(m_controlledCharacter, "Enabling input but not character controlled!");

    //        base.EnableInput();

    //        foreach (var extension in m_modules)
    //        {
    //            extension.EnableModuleInput(PlayerInput, ActiveActionMap);
    //        }
    //    }

    //    public override void DisableInput()
    //    {
    //        base.DisableInput();

    //        foreach (var extension in m_modules)
    //        {
    //            extension.DisableModuleInput(PlayerInput, ActiveActionMap);
    //        }
    //    }

    //    protected override void UpdateController()
    //    {
    //        if (m_controlledCharacter == null)
    //        {
    //            return;
    //        }

    //        foreach (var extension in m_modules)
    //        {
    //            if (!extension.IsAvailable())
    //            {
    //                continue;
    //            }

    //            extension.UpdateModule(Time.deltaTime);
    //        }
    //    }

    //    private void OnEnable()
    //    {
    //        EnableInput();

    //        foreach (var extension in m_modules)
    //        {
    //            extension.enabled = true;
    //        }
    //    }

    //    private void OnDisable()
    //    {
    //        DisableInput();

    //        foreach (var extension in m_modules)
    //        {
    //            extension.enabled = false;
    //        }
    //    }
    //}
}