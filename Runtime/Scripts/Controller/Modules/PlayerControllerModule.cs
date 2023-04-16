using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public abstract class PlayerControllerModule : MonoBehaviour
    {
        public PlayerController PlayerController { get; private set; }

        public PlayerInput PlayerInput { get; private set; }
        public Character CharacterMovement { get; private set; }

        public abstract void PlayerControllerExtensionEnableInput(PlayerInput playerInput, InputActionMap activeActionMap);

        public abstract void PlayerControllerExtensionDisableInput(PlayerInput playerInput, InputActionMap activeActionMap);

        public virtual void PlayerControllerExtensionInit(PlayerController controller)
        {
            PlayerController = controller;
            PlayerInput = controller.PlayerInput;
            CharacterMovement = controller.CharacterMovement;
        }

        public virtual void PlayerControllerExtensionUpdate(float deltaTime)
        {

        }
    }
}