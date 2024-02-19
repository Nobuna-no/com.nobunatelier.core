using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: Actions")]
    public class PlayerControllerActions : PlayerControllerModuleBase
    {
        [SerializeField] private ActionData[] m_actions;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            foreach (var action in m_actions)
            {
                action.EnableAction(activeActionMap);
            }
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            foreach (var action in m_actions)
            {
                action.DisableAction(activeActionMap);
            }
        }

        [System.Serializable]
        private class ActionData
        {
            [SerializeField] private string m_actionName = "";
            [SerializeField] private UnityEvent m_actionToExecute;

            private InputAction m_inputAction;

            public void EnableAction(InputActionMap map)
            {
                m_inputAction = map.FindAction(m_actionName);
                Debug.Assert(m_inputAction != null, $"Can't find '{m_actionName}' action");
                m_inputAction.performed += Execute;
            }

            public void DisableAction(InputActionMap map)
            {
                if (m_inputAction == null)
                {
                    return;
                }

                m_inputAction.performed -= Execute;
            }

            private void Execute(InputAction.CallbackContext obj)
            {
                m_actionToExecute?.Invoke();
            }
        }
    }
}