using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: Actions")]
    public class PlayerControllerActions : PlayerControllerModuleBase
    {
        [FormerlySerializedAs("m_actions")]
        [SerializeField] private ActionData[] m_Actions;

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            // bool needUpdate = false;
            foreach (var action in m_Actions)
            {
                action.EnableAction(activeActionMap);
                // needUpdate |= action.CanBeHoldAction;
            }

            // if (needUpdate)
            // {
            //     StartCoroutine(UpdateRoutine());
            // }
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            foreach (var action in m_Actions)
            {
                action.DisableAction(activeActionMap);
            }

            // If the coroutine hasn't started, will do nothing.
            // StopCoroutine(UpdateRoutine());
        }

        private IEnumerator UpdateRoutine()
        {
            while (true)
            {
                foreach (var action in m_Actions)
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

        private void LateUpdate()
        {
            foreach (var action in m_Actions)
            {
                if (!action.NeedUpdate)
                {
                    continue;
                }

                action.UpdateAction(Time.unscaledDeltaTime);
            }
        }

        [System.Serializable]
        private class ActionData
        {
            // TODO: To remove next version
            [HideInInspector]
            [FormerlySerializedAs("m_actionName")]
            [SerializeField] private string m_ActionName = "";

            [FormerlySerializedAs("m_action")]
            [SerializeField] private InputActionReference m_Action;

            [FormerlySerializedAs("m_onActionPerformed")]
            [SerializeField] private UnityEvent m_OnActionPerformed;

            [FormerlySerializedAs("m_onActionCancelled")]
            [SerializeField] private UnityEvent m_OnActionCancelled;

            /// <summary>
            /// OnActionHold is going to be called instead of OnActionPerformed.
            /// If action is canceled before the minimum hold threshold, OnActionPerformed will be called instead.
            /// </summary>
            [FormerlySerializedAs("m_onActionHold")]
            [SerializeField] private UnityEvent m_OnActionHold;

            [FormerlySerializedAs("m_holdTreshold")]
            [SerializeField, AllowNesting, ShowIf("CanBeHoldAction"), Range(0.01f, 1f)]
            private float m_HoldThreshold = 0.1f;

            [FormerlySerializedAs("m_RaiseActionHoldEveryFrame")]
            [SerializeField, AllowNesting, ShowIf("CanBeHoldAction")]
            private bool m_RaiseActionHoldEveryFrame = false;

            [FormerlySerializedAs("m_raiseActionPerformedOnHoldCancelled")]
            [SerializeField, AllowNesting, ShowIf("CanBeHoldAction")]
            private bool m_RaiseActionPerformedOnHoldCancelled = true;

            /// <summary>
            /// OnActionDoubleTap is called when two action performed events happen within the specified threshold time.
            /// </summary>
            [SerializeField] private UnityEvent m_OnActionDoubleTap;

            [SerializeField, AllowNesting, ShowIf("CanBeDoubleTapAction"), Range(0.01f, 1f)]
            private float m_DoubleTapThreshold = 0.3f;

            public bool CanBeHoldAction => m_OnActionHold.GetPersistentEventCount() > 0;
            public bool CanBeDoubleTapAction => m_OnActionDoubleTap.GetPersistentEventCount() > 0;

            public bool NeedUpdate { get; private set; } = false;

            private InputAction m_InputAction;
            
            [FormerlySerializedAs("m_holdDuration")]
            [SerializeField, AllowNesting, ReadOnly, ShowIf("CanBeHoldAction")]
            private float m_HoldDuration = 0f;

            private float m_LastTapTime = -1f;
            private bool m_WaitingForDoubleTap = false;

            public void EnableAction(InputActionMap map)
            {
                m_InputAction = map.FindAction(m_Action.action.name);
                Debug.Assert(m_InputAction != null, $"Can't find '{m_Action.name}' action");
                m_InputAction.performed += Perform;
                m_InputAction.canceled += Cancel;
            }

            public void DisableAction(InputActionMap map)
            {
                if (m_InputAction == null)
                {
                    return;
                }

                m_InputAction.performed -= Perform;
                m_InputAction.canceled -= Cancel;
            }

            public void UpdateAction(float deltaTime)
            {
                // Hold action update
                if (NeedUpdate)
                {
                    m_HoldDuration += deltaTime;
                    if (m_HoldDuration >= m_HoldThreshold)
                    {
                        m_OnActionHold?.Invoke();
                        NeedUpdate = m_RaiseActionHoldEveryFrame;
                    }
                }

                // Double tap update
                if (m_WaitingForDoubleTap)
                {
                    if (Time.unscaledTime - m_LastTapTime > m_DoubleTapThreshold)
                    {
                        // Tap timeout - consider it a normal action
                        m_WaitingForDoubleTap = false;
                        m_OnActionPerformed?.Invoke();
                    }
                }
            }

            private void Perform(InputAction.CallbackContext obj)
            {
                // If this action can be a double tap
                if (CanBeDoubleTapAction)
                {
                    float currentTime = Time.unscaledTime;
                    
                    if (m_WaitingForDoubleTap)
                    {
                        // Second tap detected within threshold
                        if (currentTime - m_LastTapTime <= m_DoubleTapThreshold)
                        {
                            m_WaitingForDoubleTap = false;
                            m_OnActionDoubleTap?.Invoke();
                            return;
                        }
                    }
                    
                    // First tap - delay normal action to check for double tap
                    m_LastTapTime = currentTime;
                    m_WaitingForDoubleTap = true;
                    NeedUpdate = true;
                    return;
                }
                
                // Hold action behavior takes precedence over normal action
                if (CanBeHoldAction)
                {
                    m_HoldDuration = 0;
                    NeedUpdate = true;
                    return;
                }

                // Normal action behavior
                m_OnActionPerformed?.Invoke();
            }

            private void Cancel(InputAction.CallbackContext obj)
            {
                if (CanBeHoldAction)
                {
                    NeedUpdate = false;
                    if (m_HoldDuration < m_HoldThreshold && m_RaiseActionPerformedOnHoldCancelled)
                    {
                        // If an hold action was in progress and has been canceled,
                        // it is considered as a normal Perform.
                        m_OnActionPerformed?.Invoke();
                    }
                }

                m_OnActionCancelled?.Invoke();
            }
        }
    }
}