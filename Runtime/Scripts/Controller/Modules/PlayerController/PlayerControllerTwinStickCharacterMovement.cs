using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/PlayerModule TwinStick Character Movement")]
    public class PlayerControllerTwinStickCharacterMovement: PlayerControllerModuleBase
    {
        private InputAction m_moveAction;
        private Vector2 m_lastMoveInputValue;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_moveAction = activeActionMap.FindAction("Move");
            m_moveAction.performed += OnMoveActionPerformed;
            m_moveAction.canceled += OnMoveActionCanceled;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_moveAction != null)
            {
                m_moveAction.performed -= OnMoveActionPerformed;
                m_moveAction.canceled -= OnMoveActionCanceled;
            }
        }

        public override void UpdateModule(float deltaTime)
        {
            if (m_lastMoveInputValue == Vector2.zero)
            {
                return;
            }

            Vector3 dir = new Vector3(m_lastMoveInputValue.x, 0, m_lastMoveInputValue.y);
            ControlledCharacter.Move(dir);
        }

        private void OnMoveActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = obj.ReadValue<Vector2>();
        }

        private void OnMoveActionCanceled(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = Vector2.zero;
        }
    }
}