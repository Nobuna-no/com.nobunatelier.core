using UnityEngine;

namespace NobunAtelier
{
    public class StateComponent<T> : MonoBehaviour, NobunAtelier.IState<T>
        where T : StateDefinition
    {
        [Header("State")]
#if UNITY_EDITOR
        [SerializeField, TextArea, Tooltip("EDITOR ONLY")]
        private string m_Description;
#endif
        [SerializeField]
        private T m_stateDefinition;

#if UNITY_EDITOR

        [Header("Debug")]
        public bool BreakOnEnter = false;

#endif
        private NobunAtelier.StateMachineComponent<T> m_parentStateMachine = null;
        public NobunAtelier.StateMachineComponent<T> ParentStateMachine => m_parentStateMachine;

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
        }

        public virtual void Tick(float deltaTime)
        { }

        public virtual void Exit()
        { }

        public virtual void SetState(T newState)
        {
            if (m_parentStateMachine == null)
            {
                Debug.LogWarning($"Failed to set new state [{newState}].");
                return;
            }

            m_parentStateMachine.SetState(newState);
        }

        protected virtual void Awake()
        {
            m_parentStateMachine = GetComponentInParent<NobunAtelier.StateMachineComponent<T>>();

            if (this != m_parentStateMachine)
            {
                Debug.Assert(m_parentStateMachine, $"{this} doesn't have a parent StateMachine.");
                Debug.Assert(m_stateDefinition, $"{this} doesn't have a StateDefinition.");
                m_parentStateMachine.RegisterStateComponent(this);
            }
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