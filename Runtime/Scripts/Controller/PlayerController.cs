using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/PlayerController")]
    public class PlayerController : CharacterControllerBase<PlayerControllerModuleBase>
    {
        public override bool IsAI => false;
        public PlayerInput PlayerInput => m_playerInput;

        [Header("Player Controller")]
        [SerializeField]
        protected PlayerInput m_playerInput;

        [SerializeField, Tooltip("Action map used by this controller to get bindings from.")]
        private string m_actionMapName = "Player";

        protected InputActionMap ActionMap => m_actionMap;
        protected string ActionMapName => m_actionMapName;
        protected InputActionMap ActiveActionMap => m_actionMap;
        public bool IsInputReady { get; private set; } = false;

        private InputActionMap m_actionMap;

        protected override void Awake()
        {
            base.Awake();

            CapturePlayerInputAndSetBehaviour();
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

        public override void EnableInput()
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

        public override void DisableInput()
        {
            m_playerInput.DeactivateInput();
            m_actionMap = null;
            IsInputReady = false;

            foreach (var extension in m_modules)
            {
                extension.DisableModuleInput(PlayerInput, ActiveActionMap);
            }
        }

        private bool IsPlayerInputValid()
        {
            return m_playerInput != null;
        }

        protected override void OnDestroy()
        {
            UnMountPlayerInput();
        }

        private void Update()
        {
            UpdateController(Time.deltaTime);
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
                m_playerInput = GetComponentInParent<PlayerInput>();
            }

            if (m_playerInput)
            {
                m_playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            }
        }
    }
}