using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: Jump")]
    public class PlayerControllerJump : PlayerControllerModuleBase
    {
        [SerializeField]
        private string m_actionName = "Jump";

        private InputAction m_jumpAction;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_jumpAction = activeActionMap.FindAction(m_actionName);
            Debug.Assert(m_jumpAction != null, $"Can't find '{m_actionName}' action");
            m_jumpAction.performed += M_jumpAction_performed;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_jumpAction != null)
            {
                m_jumpAction.performed -= M_jumpAction_performed;
            }
        }

        private void M_jumpAction_performed(InputAction.CallbackContext obj)
        {
            var character = ModuleOwner.ControlledCharacter as Character;

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