using NaughtyAttributes;
using NUnit.Framework.Internal;
using System;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class StateComponent : MonoBehaviour
    {
        // Reflection SetState, required to work with state module.
        public abstract void SetState(Type newState, StateDefinition stateDefinition);
    }

    public class StateComponent<T> : StateComponent, NobunAtelier.IState<T>
        where T : StateDefinition
    {
        [Header("State")]
#if UNITY_EDITOR
        [SerializeField, TextArea, Tooltip("EDITOR ONLY")]
        private string m_Description;
#endif
        [SerializeField]
        private T m_stateDefinition;

        [SerializeField]
        protected StateComponentModule[] m_stateModules;
        [SerializeField]
        private bool m_autoCaptureStateModule = true;

#if UNITY_EDITOR
        [Header("Debug")]
        public bool BreakOnEnter = false;

#endif
        private NobunAtelier.StateMachineComponent<T> m_parentStateMachine = null;
        public NobunAtelier.StateMachineComponent<T> ParentStateMachine => m_parentStateMachine;

        protected bool HasStateModule => m_stateModules != null && m_stateModules.Length > 0;
        private Type m_genericState;
        private Type m_stateDefinitionType;
        private System.Reflection.MethodInfo m_setStateMethod;

        // Return the definition defining the component.
        public T GetStateDefinition()
        {
            return m_stateDefinition;
        }

        public virtual void Enter()
        {
#if UNITY_EDITOR
            if (BreakOnEnter)
            {
                Debug.Break();
                Debug.Log($"{this}: Debug Break!");
            }
#endif

            if (!HasStateModule)
            {
                return;
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
            if (m_parentStateMachine == null || this == m_parentStateMachine)
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

            m_parentStateMachine = GetComponentInParent<NobunAtelier.StateMachineComponent<T>>();

            if (this != m_parentStateMachine)
            {
                Debug.Assert(m_parentStateMachine, $"{this} doesn't have a parent StateMachine.");
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