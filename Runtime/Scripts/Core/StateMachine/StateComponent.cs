using NaughtyAttributes;
using NUnit.Framework.Internal;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class StateComponent : MonoBehaviour
    {
        // Reflection SetState, required to work with state module.
        public abstract void SetState(Type newState, StateDefinition stateDefinition);
    }

    public class StateComponent<T, TCollection> : StateComponent, NobunAtelier.IState<T>
        where T : StateDefinition
        where TCollection : DataCollection
    {
        [Header("State")]

        [SerializeField, TextArea]
        private string m_Description;

        [SerializeField, ShowIf("HasParentStateMachine")]
        private T m_stateDefinition;

        [SerializeField]
        protected StateComponentModule[] m_stateModules;
        [SerializeField]
        private bool m_autoCaptureStateModule = true;

        [Header("Debug")]
        [SerializeField]
        protected bool m_logDebug = false;

        private NobunAtelier.StateMachineComponent<T, TCollection> m_parentStateMachine = null;
        public NobunAtelier.StateMachineComponent<T, TCollection> ParentStateMachine => m_parentStateMachine;

        protected bool HasStateModule => m_stateModules != null && m_stateModules.Length > 0;
        private Type m_genericState;
        private Type m_stateDefinitionType;
        private System.Reflection.MethodInfo m_setStateMethod;

        private bool HasParentStateMachine => m_parentStateMachine != null;

        // Return the definition defining the component.
        public T GetStateDefinition()
        {
            return m_stateDefinition;
        }

        public virtual void Enter()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.Enter");
            }

            if (!HasStateModule)
            {
                return;
            }
            else if (m_logDebug)
            {
                Debug.Log($"{this.name}.Enter: Has {m_stateModules.Length} state module(s).");
            }

            for (int i = 0, c = m_stateModules.Length; i < c; i++)
            {
                m_stateModules[i].Enter();
            }
        }

        public virtual void Tick(float deltaTime)
        {
            if (!HasStateModule)
            {
                return;
            }

            for (int i = 0, c = m_stateModules.Length; i < c; i++)
            {
                m_stateModules[i].Tick(deltaTime);
            }
        }

        public virtual void Exit()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.Exit");
            }

            if (!HasStateModule)
            {
                return;
            }

            for (int i = 0, c = m_stateModules.Length; i < c; i++)
            {
                m_stateModules[i].Exit();
            }
        }

        public virtual void SetState(T newState)
        {
            if (m_parentStateMachine == null)
            {
                Debug.LogError($"Failed to set new state [{newState}].");
                return;
            }

            m_parentStateMachine.SetState(newState);
        }

        public sealed override void SetState(Type newStateType, StateDefinition stateDefinition)
        {
            // Check if the specified newStateType matches the StateDefinition type
            if (m_stateDefinitionType != newStateType)
            {
                Debug.LogError($"Types mismatched! StateDefinition[{m_stateDefinitionType}] is different from {newStateType}!");
                return;
            }

            // Check if the specified newStateType matches the StateDefinition type
            if (m_setStateMethod != null && m_setStateMethod.GetParameters().Length == 1 && m_setStateMethod.GetParameters()[0].ParameterType == typeof(T))
            {
                // Call SetState(T newState) on the found StateComponent<T> using reflection
                m_setStateMethod.Invoke(this, new object[] { stateDefinition });
            }
            else
            {
                Debug.LogWarning($"Specified StateDefinition [{newStateType}] does not match the StateComponent type.");
            }
        }

        protected virtual void Awake()
        {
            if (!this.enabled)
            {
                return;
            }

            if (transform.parent != null)
            {
                m_parentStateMachine = transform.parent.GetComponentInParent<NobunAtelier.StateMachineComponent<T, TCollection>>(true);
            }

            if (m_parentStateMachine != null)
            {
                Debug.Assert(m_stateDefinition, $"{this} doesn't have a StateDefinition.");
                m_parentStateMachine.RegisterStateComponent(this);
            }


            if (m_autoCaptureStateModule)
            {
                CaptureStateModule();
            }

            if (!HasStateModule)
            {
                var availableSM = GetComponents<StateComponentModule>();
                if (availableSM != null && availableSM.Length > 0)
                {
                    Debug.LogWarning($"{this.name}: Doesn't have any of the {availableSM.Length} available state module(s).");
                }
                return;
            }

            InitializeReflectionFields();

            for (int i = 0, c = m_stateModules.Length; i < c; i++)
            {
                m_stateModules[i].Init(this);
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (transform.parent != null)
            {
                m_parentStateMachine = transform.parent.GetComponentInParent<NobunAtelier.StateMachineComponent<T, TCollection>>(true);
            }

            if (m_stateDefinition != null)
            {
                // if first frame we force the state definition description except if it is empty
                if (gameObject.name != $"state-{m_stateDefinition.name}")
                {
                    gameObject.name = $"state-{m_stateDefinition.name}";

                    if (!string.IsNullOrEmpty(m_stateDefinition.Description))
                    {
                        m_Description = m_stateDefinition.Description;
                    }
                }
                else if (string.IsNullOrEmpty(m_Description) && !string.IsNullOrEmpty(m_stateDefinition.Description))
                {
                    m_Description = m_stateDefinition.Description;
                }
                else if (m_Description != m_stateDefinition.Description)
                {
                    m_stateDefinition.Editor_SetDescription(m_Description);
                }
            }

            if (m_autoCaptureStateModule)
            {
                CaptureStateModule();
            }
        }
#endif

        private void InitializeReflectionFields()
        {
            m_genericState = GetType();
            while (m_genericState != null)
            {
                if (m_genericState.BaseType == null)
                {
                    return;
                }

                m_genericState = m_genericState.BaseType;
                if (m_genericState.IsGenericType)
                {
                    break;
                }
            }

            m_stateDefinitionType = m_genericState.GetGenericArguments()[0];

            // Get the SetState method of the StateComponent<T>
            m_setStateMethod = GetType().GetMethod("SetState", new Type[] { m_stateDefinitionType });
        }

        [Button(enabledMode: EButtonEnableMode.Editor)]
        private void CaptureStateModule()
        {
            m_stateModules = GetComponents<StateComponentModule>();
        }

        // Called by StateMachine.OnGUI
        public virtual void StateDebugGUI()
        {
            IMGUIUtility.DrawTitle(this.ToString());
            IMGUIUtility.DrawLabelValue("Definition", m_stateDefinition.name);
            IMGUIUtility.DrawLabelValue("Parent", m_parentStateMachine ? m_parentStateMachine.name : "null");
        }
    }
}