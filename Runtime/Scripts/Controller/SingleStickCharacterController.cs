using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class SingleStickCharacterController : PlayerController
    {
        private InputAction m_moveAction;
        private Vector2 m_lastMoveInputValue;

        public override void EnableInput()
        {
            base.EnableInput();
            m_moveAction = ActionMap.FindAction("Move");
            m_moveAction.performed += OnMoveActionPerformed;
            m_moveAction.canceled += OnMoveActionCanceled;
        }

        public override void DisableInput()
        {
            m_moveAction.performed -= OnMoveActionPerformed;
            m_moveAction.canceled -= OnMoveActionCanceled;
            base.DisableInput();
        }

        protected override void ControllerUpdate()
        {
            if (m_lastMoveInputValue != Vector2.zero)
            {
                Vector3 dir = new Vector3(m_lastMoveInputValue.x, 0, m_lastMoveInputValue.y);
                m_characterMovement.Move(dir, Time.deltaTime);
                m_characterMovement.Rotate(dir);
            }
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