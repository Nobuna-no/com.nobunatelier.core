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
            var character = PlayerController.ControlledCharacter as AtelierCharacter;

            if (character == null)
            {
                return;
            }

            if (character.TryGetVelocityModule<CharacterBasicJumpVelocity>(out var module))
            {
                module.DoJump();
            }
            else if (character.TryGetVelocityModule<CharacterProceduralJumpVelocity>(out var module2))
            {
                module2.DoJump();
            }
            else
            {
                Debug.LogWarning($"Trying to jump but no CharacterBasicJumpVelocity module attached on the controlled character: {ControlledCharacter}");
            }
        }
    }
}