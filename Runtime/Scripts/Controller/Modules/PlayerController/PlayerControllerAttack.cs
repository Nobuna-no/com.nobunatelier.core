using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/PlayerModule Attack")]
    public class PlayerControllerAttack : PlayerControllerModuleBase
    {
        [SerializeField]
        private string m_actionName = "Attack";

        private InputAction m_attackAction;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_attackAction = activeActionMap.FindAction(m_actionName);
            Debug.Assert(m_attackAction != null, $"Can't find '{m_actionName}' action");
            m_attackAction.performed += AttackAction_performed;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_attackAction != null)
            {
                m_attackAction.performed -= AttackAction_performed;
            }
        }

        private void AttackAction_performed(InputAction.CallbackContext obj)
        {
            if (ModuleOwner.ControlledCharacter == null)
            {
                return;
            }

            // if (character.TryGetAbilityModule<CharacterSimpleTargetingAbility>(out var module))
            // {
            //     module.NextTarget();
            // }
        }
    }
}