using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class PlayerControllerModule_Jump : PlayerControllerModule
    {
        [SerializeField]
        private string m_actionName = "Jump";

        private InputAction m_jumpAction;

        public override void PlayerControllerExtensionEnableInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_jumpAction = activeActionMap.FindAction(m_actionName);
            m_jumpAction.performed += M_jumpAction_performed;
        }


        public override void PlayerControllerExtensionDisableInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {

        }

        private void M_jumpAction_performed(InputAction.CallbackContext obj)
        {
            var character = PlayerController.CharacterMovement as AtelierCharacter;

            if (character == null)
            {
                return;
            }

            var module = character.GetModule_Concept<CharacterBasicJumpVelocity>();
            Debug.Assert(module != null);
            module.DoJump();
        }
    }
}