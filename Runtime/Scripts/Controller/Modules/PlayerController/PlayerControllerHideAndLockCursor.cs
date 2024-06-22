using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    // Can be extended to add an action that re-enables it temporally (like holding Alt...)
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: Hide And Lock Cursor")]
    public class PlayerControllerHideAndLockCursor : PlayerControllerModuleBase
    {
        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            CursorLockAndHide(true);
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
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