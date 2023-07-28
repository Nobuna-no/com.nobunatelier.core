using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/PlayerModule Targeting")]
    public class PlayerControllerTargeting : PlayerControllerModuleBase
    {
        [SerializeField]
        private string m_actionName = "NextTarget";

        private InputAction m_nextTargetAction;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_nextTargetAction = activeActionMap.FindAction(m_actionName);
            Debug.Assert(m_nextTargetAction != null, $"Can't find '{m_actionName}' action");
            m_nextTargetAction.performed += NextTargetAction_performed;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_nextTargetAction != null)
            {
                m_nextTargetAction.performed -= NextTargetAction_performed;
            }
        }

        private void NextTargetAction_performed(InputAction.CallbackContext obj)
        {
            var character = ModuleOwner.ControlledCharacter as Character;

            if (character == null)
            {
                return;
            }

            if (character.TryGetAbilityModule<CharacterSimpleTargetingAbility>(out var module))
            {
                module.NextTarget();
            }
        }
    }
}
