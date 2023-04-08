using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public abstract class PlayerController : CharacterController
    {
        public override bool IsAI => false;

        public PlayerInput PlayerInput => m_playerInput;
        protected InputActionMap ActionMap => m_actionMap;
        protected string ActionMapName => m_actionMapName;

        [SerializeField]
        protected PlayerInput m_playerInput;

        [SerializeField, Tooltip("Action map used by this controller to get bindings from.")]
        private string m_actionMapName = "Player";

        private InputActionMap m_actionMap;

        [SerializeField, ShowIf("IsPlayerInputValid")]
        private bool m_mountInputOnAwake = false;

        public bool IsInputReady { get; private set; } = false;

        public virtual void MountPlayerInput(PlayerInput player, bool enableInput = true)
        {
            m_playerInput = player;

            if (enableInput)
            {
                EnableInput();
            }
        }

        public virtual void PlayerInputUnMount()
        {
            DisableInput();
            m_playerInput = null;
        }

        public virtual void EnableInput()
        {
            Debug.Assert(m_playerInput != null, $"[{Time.frameCount}] {this}: PlayerInput is required");

            m_playerInput.ActivateInput();
            m_playerInput.SwitchCurrentActionMap(m_actionMapName);
            m_actionMap = m_playerInput.actions.FindActionMap(m_actionMapName);

            IsInputReady = true;
        }

        public virtual void DisableInput()
        {
            m_playerInput.DeactivateInput();
            m_actionMap = null;
            IsInputReady = false;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_playerInput && m_mountInputOnAwake)
            {
                EnableInput();
            }
        }

        private bool IsPlayerInputValid()
        {
            return m_playerInput != null;
        }

        protected virtual void OnDestroy()
        {
            PlayerInputUnMount();
        }
    }
}