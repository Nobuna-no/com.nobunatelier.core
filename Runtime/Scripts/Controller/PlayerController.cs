using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller")]
    public class PlayerController : CharacterControllerBase<PlayerControllerModuleBase>
    {
        public override bool IsAI => false;
        public PlayerInput PlayerInput => m_PlayerInput;

        [Header("Player Controller")]
        [SerializeField]
        [FormerlySerializedAs("m_playerInput")]
        protected PlayerInput m_PlayerInput;

        [SerializeField, Tooltip("Action map used by this controller to get bindings from.")]
        [FormerlySerializedAs("m_actionMapName")]
        private string m_ActionMapName = "Player";

        protected InputActionMap ActionMap => m_ActionMap;
        protected string ActionMapName => m_ActionMapName;
        protected InputActionMap ActiveActionMap => m_ActionMap;
        public bool IsInputReady { get; private set; } = false;

        private InputActionMap m_ActionMap;

        protected override void Awake()
        {
            base.Awake();

            CapturePlayerInputAndSetBehaviour();
        }

        public virtual void MountPlayerInput(PlayerInput player, bool enableInput = true)
        {
            m_PlayerInput = player;

            if (enableInput)
            {
                EnableInput();
            }
        }

        public virtual void UnMountPlayerInput()
        {
            DisableInput();
            m_PlayerInput = null;
        }

        public override void EnableInput()
        {
            Debug.Assert(m_PlayerInput != null, $"[{Time.frameCount}] {this}: PlayerInput is required");

            m_PlayerInput.ActivateInput();
            m_PlayerInput.SwitchCurrentActionMap(m_ActionMapName);
            m_ActionMap = m_PlayerInput.actions.FindActionMap(m_ActionMapName);

            IsInputReady = true;

            foreach (var extension in m_Modules)
            {
                if (!extension.enabled)
                {
                    continue;
                }

                extension.EnableModuleInput(PlayerInput, ActiveActionMap);
            }
        }

        public override void DisableInput()
        {
            m_PlayerInput.DeactivateInput();
            m_ActionMap = null;
            IsInputReady = false;

            foreach (var extension in m_Modules)
            {
                if (!extension.enabled)
                {
                    continue;
                }

                extension.DisableModuleInput(PlayerInput, ActiveActionMap);
            }
        }

        private bool IsPlayerInputValid()
        {
            return m_PlayerInput != null;
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
            if (m_PlayerInput == null)
            {
                m_PlayerInput = GetComponentInParent<PlayerInput>();
            }

            if (m_PlayerInput)
            {
                m_PlayerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            }
        }
    }
}