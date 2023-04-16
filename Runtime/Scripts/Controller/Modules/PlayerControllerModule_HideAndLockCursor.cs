using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    // Can be extended to add an action that re-enables it temporally (like holding Alt...)
    public class PlayerControllerModule_HideAndLockCursor : PlayerControllerModule
    {
        public override void PlayerControllerExtensionEnableInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            CursorLockAndHide(true);
        }

        public override void PlayerControllerExtensionDisableInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            CursorLockAndHide(false);
        }

        public void CursorLockAndHide(bool enable)
        {
            if (enable)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}