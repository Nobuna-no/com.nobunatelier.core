using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class PlayerControllerModule_TwinStickCharacterRotation: PlayerControllerModule
    {
        [SerializeField]
        private Camera m_camera;
        [SerializeField]
        private string m_lookActionName = "Look";

        private InputAction m_lookAction;
        private Vector2 m_lastLookInputValue;

        public override void PlayerControllerExtensionEnableInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_camera == null)
            {
                Debug.LogWarning("No camera set, using main camera as reference...");
                m_camera = Camera.main;
            }

            m_lookAction = activeActionMap.FindAction(m_lookActionName);

            m_lookAction.performed += OnLookActionPerformed;
        }

        public override void PlayerControllerExtensionDisableInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_lookAction != null)
            {
                m_lookAction.performed -= OnLookActionPerformed;
            }

            m_lookAction = null;
        }

        public override void PlayerControllerExtensionUpdate(float deltaTime)
        {
            base.PlayerControllerExtensionUpdate(deltaTime);

            if (m_lastLookInputValue != Vector2.zero)
            {
                if (PlayerInput.currentControlScheme == "Gamepad")
                {
                    CharacterMovement.Rotate(m_lastLookInputValue);
                }
                else
                {
                    Vector3 direction = Input.mousePosition - m_camera.WorldToScreenPoint(CharacterMovement.transform.position);
                    CharacterMovement.Rotate(direction.normalized);
                }
            }
        }

        private void OnLookActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastLookInputValue = obj.ReadValue<Vector2>();
        }
    }
}