using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class TwinStickCharacterController : PlayerController
    {
        [SerializeField]
        private Camera m_camera;

        protected InputActionMap m_playerActionMap;

        private InputAction m_moveAction;
        private InputAction m_lookAction;

        private Vector2 m_lastMoveInputValue;
        private Vector2 m_lastLookInputValue;

        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(m_characterMovement != null);
            Debug.Assert(PlayerInput != null);

            m_playerActionMap = PlayerInput.actions.FindActionMap("Player");
            m_moveAction = m_playerActionMap.FindAction("Move");
            m_lookAction = m_playerActionMap.FindAction("Look");

            if (m_camera == null)
            {
                m_camera = Camera.main;
            }
        }

        protected override void ControllerUpdate()
        {
            if (m_lastMoveInputValue != Vector2.zero)
            {
                Vector3 dir = new Vector3(m_lastMoveInputValue.x, 0, m_lastMoveInputValue.y);
                m_characterMovement.Move(dir, Time.deltaTime);
            }
        }

        protected override void ControllerFixedUpdate()
        {
            if (m_lastLookInputValue != Vector2.zero)
            {
                if (PlayerInput.currentControlScheme == "Gamepad")
                {
                    m_characterMovement.StickAim(m_lastLookInputValue);
                }
                else
                {
                    Vector3 direction = Input.mousePosition - m_camera.WorldToScreenPoint(m_characterMovement.transform.position);
                    m_characterMovement.MouseAim(direction.normalized);
                }
            }
        }

        protected virtual void OnEnable()
        {
            m_moveAction.performed += OnMoveActionPerformed;
            m_moveAction.canceled += OnMoveActionCanceled;

            m_lookAction.performed += OnLookActionPerformed;
        }

        protected virtual void OnDisable()
        {
            m_moveAction.performed -= OnMoveActionPerformed;
            m_moveAction.canceled -= OnMoveActionCanceled;

            m_lookAction.performed -= OnLookActionPerformed;
        }

        private void OnMoveActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = obj.ReadValue<Vector2>();
        }

        private void OnMoveActionCanceled(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = Vector2.zero;
        }

        private void OnLookActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastLookInputValue = obj.ReadValue<Vector2>();
        }
    }
}