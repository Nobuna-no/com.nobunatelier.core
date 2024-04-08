using System.Collections;
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
            bool needUpdate = false;
            foreach (var action in m_actions)
            {
                action.EnableAction(activeActionMap);
                needUpdate |= action.CanBeHoldAction;
            }

            if (needUpdate)
            {
                StartCoroutine(UpdateRoutine());
            }
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            foreach (var action in m_actions)
            {
                action.DisableAction(activeActionMap);
            }

            // If the coroutine hasn't started, will do nothing.
            StopCoroutine(UpdateRoutine());
        }

        private IEnumerator UpdateRoutine()
        {
            while (true)
            {
                foreach (var action in m_actions)
                {
                    if (!action.NeedUpdate)
                    {
                        continue;
                    }

                    action.UpdateAction(Time.unscaledDeltaTime);
                }

                // In case user use slowmo style, we don't want player input to be impacted.
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
        }

        [System.Serializable]
        private class ActionData
        {
            [SerializeField] private string m_actionName = "";
            [SerializeField] private UnityEvent m_onActionPerformed;
            [SerializeField] private UnityEvent m_onActionCancelled;
            /// <summary>
            /// OnActionHold is going to be called instead of OnActionPerformed.
            /// If action is canceled before the minimum hold threshold, OnActionPerformed will be called instead.
            /// </summary>
            [SerializeField]
            private UnityEvent m_onActionHold;
            [SerializeField, AllowNesting, ShowIf("CanBeHoldAction"), Range(0.01f, 1f)]
            private float m_holdTreshold = 0.1f;
            [SerializeField, AllowNesting, ShowIf("CanBeHoldAction")]
            private bool m_raiseActionPerformedOnHoldCancelled = true;

            public bool CanBeHoldAction => m_onActionHold.GetPersistentEventCount() > 0;

            public bool NeedUpdate { get; private set; } = false;

            private InputAction m_inputAction;
            private float m_holdDuration = 0f;

            public void EnableAction(InputActionMap map)
            {
                m_inputAction = map.FindAction(m_actionName);
                Debug.Assert(m_inputAction != null, $"Can't find '{m_actionName}' action");
                m_inputAction.performed += Perform;
                m_inputAction.canceled += Cancel;
            }

            public void DisableAction(InputActionMap map)
            {
                if (m_inputAction == null)
                {
                    return;
                }

                m_inputAction.performed -= Perform;
                m_inputAction.canceled -= Cancel;
            }

            public void UpdateAction(float deltaTime)
            {
                m_holdDuration += deltaTime;
                if (m_holdDuration >= m_holdTreshold)
                {
                    m_onActionHold?.Invoke();
                    NeedUpdate = false;
                }
            }

            private void Perform(InputAction.CallbackContext obj)
            {
                if (CanBeHoldAction)
                {
                    m_holdDuration = 0;
                    NeedUpdate = true;
                    return;
                }

                m_onActionPerformed?.Invoke();
            }

            private void Cancel(InputAction.CallbackContext obj)
            {
                if (CanBeHoldAction)
                {
                    NeedUpdate = false;
                    if (m_holdDuration < m_holdTreshold && m_raiseActionPerformedOnHoldCancelled)
                    {
                        // If an hold action was in progress and has been canceled,
                        // it is considered as a normal Perform.
                        m_onActionPerformed?.Invoke();
                    }
                }

                m_onActionCancelled?.Invoke();
            }
        }
    }
}