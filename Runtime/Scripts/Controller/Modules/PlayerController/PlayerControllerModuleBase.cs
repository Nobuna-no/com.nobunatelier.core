using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public abstract class PlayerControllerModuleBase : CharacterControllerModuleBase<PlayerController>
    {
        public PlayerInput PlayerInput { get; private set; }

        public abstract void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap);

        public abstract void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap);

        public override void InitModule(CharacterControllerBase controller)
        {
            base.InitModule(controller);

            PlayerInput = ModuleOwner.PlayerInput;
        }
    }
}