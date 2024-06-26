using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: TwinStick Rotation")]
    public class PlayerControllerTwinStickCharacterRotation : PlayerControllerModuleBase
    {
        [SerializeField]
        private Camera m_camera;

        [SerializeField]
        private string m_lookActionName = "Look";
        [SerializeField]
        private bool m_useMouseWorldToScreen = true;

        private InputAction m_lookAction;
        private Vector2 m_lastLookInputValue;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_useMouseWorldToScreen && m_camera == null)
            {
                Debug.LogWarning("No camera set, using main camera as reference...", this);
                m_camera = Camera.main;
            }

            m_lookAction = activeActionMap.FindAction(m_lookActionName);

            m_lookAction.performed += OnLookActionPerformed;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_lookAction != null)
            {
                m_lookAction.performed -= OnLookActionPerformed;
            }

            m_lookAction = null;
        }

        public override void UpdateModule(float deltaTime)
        {
            base.UpdateModule(deltaTime);

            if (m_lastLookInputValue != Vector2.zero)
            {
                if (!m_useMouseWorldToScreen || PlayerInput.currentControlScheme == "Gamepad")
                {
                    ControlledCharacter.Rotate(m_lastLookInputValue);
                }
                else
                {
                    Vector3 direction = Input.mousePosition - m_camera.WorldToScreenPoint(ControlledCharacter.transform.position);
                    ControlledCharacter.Rotate(direction.normalized);
                }
            }
        }

        private void OnLookActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastLookInputValue = obj.ReadValue<Vector2>();
        }
    }
}