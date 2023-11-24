using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier
{
    [DefaultExecutionOrder(100)]
    public class StateMachineComponent<T, TCollection> : StateComponent<T, TCollection>
        where T : StateDefinition
        where TCollection : DataCollection
    {
        public T CurrentStateDefinition => m_activeStateDefinition;
        public bool IsPaused { get; set; } = false;

        [Header("State Machine")]
#if UNITY_EDITOR
        [SerializeField, ShowIf("IsMainStateMachine")]
        private TCollection m_referenceStateCollection;

#endif

        [SerializeField]
        private T m_initialStateDefinition;

        [SerializeField, ShowIf("IsMainStateMachine")]
        private bool m_enterInitialStateOnStart = false;

        [SerializeField, ShowIf("HasParentStateMachine")]
        private T m_nextStateOnStateMachineExit;

        [Header("Debug")]
        [SerializeField]
        protected bool m_displayDebug = false;

        private Dictionary<T, StateComponent<T, TCollection>> m_statesMap = new Dictionary<T, StateComponent<T, TCollection>>();
        private T m_activeStateDefinition = null;
        private T m_activeDebugState = null;
        private Vector2 m_debugStateScrollPosition = Vector2.zero;

        private bool HasParentStateMachine => ParentStateMachine != null;
        private bool IsMainStateMachine => ParentStateMachine == null;

        protected virtual void Start()
        {
            this.enabled = ParentStateMachine == null;

            if (m_enterInitialStateOnStart)
            {
                Enter();
            }
        }

        public void ToggleDebug()
        {
            m_displayDebug = !m_displayDebug;
        }

        public void RegisterStateComponent(StateComponent<T, TCollection> state)
        {
            m_statesMap.Add(state.GetStateDefinition(), state);
        }

        public void StartFromScratch()
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            // this.enabled = true;
            IsPaused = false;
            m_activeStateDefinition = GetInitialStateDefinition();
            Enter();
        }

        public T GetInitialStateDefinition()
        {
            return m_initialStateDefinition;
        }

        public void Sleep()
        {
            // this.enabled = false;
            IsPaused = true;
        }

        public override void SetState(T newState)
        {
            if (IsPaused)
            {
                Debug.LogWarning($"Trying to set state {newState} in {gameObject} but StateMachine is paused. Skipped.");
                return;
            }

            if (newState == m_activeStateDefinition)
            {
                return;
            }

            if (!m_statesMap.ContainsKey(newState))
            {
                base.SetState(newState);
                return;
            }

            if (m_activeStateDefinition != null)
            {
                m_statesMap[m_activeStateDefinition].Exit();
            }
            m_activeStateDefinition = newState;
            m_statesMap[m_activeStateDefinition].Enter();
        }

        public override void Enter()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.Enter");
            }

            if (ParentStateMachine != null)
            {
                // this.enabled = true;
                IsPaused = false;
            }

            if (HasStateModule)
            {
                if (m_logDebug)
                {
                    Debug.Log($"{this.name}.Enter: Starting {m_stateModules.Length} state module(s).");
                }

                for (int i = 0, c = m_stateModules.Length; i < c; i++)
                {
                    m_stateModules[i].Enter();
                }
            }

            if (GetInitialStateDefinition() != null)
            {
                m_activeStateDefinition = GetInitialStateDefinition();
                while (m_activeStateDefinition.RequiredPriorState != null)
                {
                    Debug.LogWarning($"Required condition <b>{m_activeStateDefinition.RequiredPriorState.name}</b> for state <b>{m_activeStateDefinition.name}</b>. " +
                        $"Rolling back state to <b>{m_activeStateDefinition.RequiredPriorState.name}</b>.");
                    m_activeStateDefinition = m_activeStateDefinition.RequiredPriorState as T;
                }

                if (!m_statesMap.ContainsKey(m_activeStateDefinition))
                {
                    Debug.LogError($"State machine doesn't have a valid StateComponent for state <b>{m_activeStateDefinition.name}</b>");
                }
                m_statesMap[m_activeStateDefinition].Enter();
            }
        }

        public override void Exit()
        {
            if (HasStateModule)
            {
                for (int i = 0, c = m_stateModules.Length; i < c; i++)
                {
                    m_stateModules[i].Exit();
                }
            }

            if (m_activeStateDefinition != null)
            {
                m_statesMap[m_activeStateDefinition].Exit();
            }
        }

        public virtual void ExitStateMachine()
        {
            if (ParentStateMachine != null)
            {
                ParentStateMachine.SetState(m_nextStateOnStateMachineExit);
            }
            else
            {
                Debug.LogError($"{this.name}: Cannot stop main state machine");
            }
        }

        public override void Tick(float deltaTime)
        {
            if (IsPaused || ParentStateMachine != null && ParentStateMachine.IsPaused)
            {
                return;
            }

            base.Tick(deltaTime);

            if (m_activeStateDefinition == null)
            {
                return;
            }

            m_statesMap[m_activeStateDefinition].Tick(deltaTime);
        }

#if UNITY_EDITOR

        public TCollection ReferenceStateCollection
        {
            get
            {
                if (IsMainStateMachine)
                {
                    return m_referenceStateCollection;
                }
                else
                {
                    return ParentStateMachine.ReferenceStateCollection;
                }
            }
        }

        // very slow and not optimized, but whatever ;D
        public int GetStateDefinitionRefCount(StateComponent<T, TCollection> source, T definition)
        {
            int useCount = 0;
            var children = transform.GetComponentsInChildren<StateComponent<T, TCollection>>();

            for (int i = 0, c = children.Length; i < c; ++i)
            {
                if (children[i].GetStateDefinition() == definition)
                {
                    ++useCount;
                }
            }

            return useCount;
        }

        // very slow and not optimized, but whatever ;D
        public void PingStateDefinitionRef(T definition, int objectApparitionOrder)
        {
            var children = transform.GetComponentsInChildren<StateComponent<T, TCollection>>();

            if (objectApparitionOrder < 0 || objectApparitionOrder >= children.Length)
            {
                return;
            }

            int apparitionCount = 0;
            for (int i = 0, c = children.Length; i < c; ++i)
            {
                if (children[i].GetStateDefinition() == definition)
                {
                    if (apparitionCount == objectApparitionOrder)
                    {
                        EditorGUIUtility.PingObject(children[i].gameObject);
                        break;
                    }

                    ++apparitionCount;
                }
            }
        }

#endif

        protected virtual void OnGUI()
        {
            if (!Application.isPlaying || !m_displayDebug)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.window);
            {
                IMGUIUtility.DrawTitle(this.ToString());
                IMGUIUtility.DrawLabelValue("Current state", m_activeStateDefinition ? m_activeStateDefinition.name : "None");
                IMGUIUtility.DrawLabelValue("Is paused", IsPaused.ToString());

                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        foreach (var a in m_statesMap)
                        {
                            if (GUILayout.Toggle(m_activeDebugState == a.Key, a.Key.name))
                            {
                                m_activeDebugState = a.Key;
                            }
                            else if (m_activeDebugState == a.Key)
                            {
                                m_activeDebugState = null;
                            }
                        }
                    }
                    GUILayout.EndVertical();

                    if (m_activeDebugState != null)
                    {
                        m_debugStateScrollPosition = GUILayout.BeginScrollView(m_debugStateScrollPosition, GUI.skin.box);
                        m_statesMap[m_activeDebugState].StateDebugGUI();
                        GUILayout.EndScrollView();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}