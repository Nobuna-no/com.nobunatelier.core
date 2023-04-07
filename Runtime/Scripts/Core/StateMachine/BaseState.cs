using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class BaseState<T> : StateComponent<T>
        where T : NobunAtelier.StateDefinition
    {
        // To improve by creating an intermediate that have only UnityEvent and one with timed transition
        [Header("Base State")]
        [SerializeField]
        private bool m_transitionToNextState = false;

        [SerializeField, ShowIf("m_transitionToNextState")]
        private T m_nextState;

        [SerializeField, ShowIf("m_transitionToNextState")]
        private float m_stateDurationInSeconds = -1f;

        private float m_timeBeforeNextState = -1f;

        [Header("Events")]
        public UnityEvent OnStateEnter;

        public UnityEvent OnStateExit;

        public override void Enter()
        {
            base.Enter();
            if (m_transitionToNextState)
            {
                m_timeBeforeNextState = m_stateDurationInSeconds;
            }
            OnStateEnter?.Invoke();
        }

        public override void Tick(float deltaTime)
        {
            if (!m_transitionToNextState)
            {
                return;
            }

            m_timeBeforeNextState -= deltaTime;
            if (m_timeBeforeNextState <= 0)
            {
                SetState(m_nextState);
            }
        }

        public override void Exit()
        {
            base.Exit();
            OnStateExit?.Invoke();
        }
    }
}