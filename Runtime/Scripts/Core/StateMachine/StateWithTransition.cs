using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class StateWithTransition<T, TCollection> : StateComponent<T, TCollection>
        where T : NobunAtelier.StateDefinition
        where TCollection : DataCollection
    {
        private enum NextStateTransitionType
        {
            Delay,
            Manual,
        }

        // To improve by creating an intermediate that have only UnityEvent and one with timed transition
        [Header("Next State")]
        [SerializeField]
        private NextStateTransitionType TransitionType = NextStateTransitionType.Manual;

        [SerializeField, ShowIf("DisplayNextState")]
        private T m_nextState;

        [SerializeField, ShowIf("DisplayDelay")]
        private float m_stateDurationInSeconds = -1f;

        private float m_timeBeforeNextState = -1f;

#if UNITY_EDITOR
        // Used by NaughtyAttributes.ShowIf
        private bool DisplayDelay => TransitionType == NextStateTransitionType.Delay;
        // Used by NaughtyAttributes.ShowIf
        private bool DisplayNextState => TransitionType != NextStateTransitionType.Manual;
#endif

        public override void Enter()
        {
            base.Enter();
            if (TransitionType == NextStateTransitionType.Delay)
            {
                m_timeBeforeNextState = m_stateDurationInSeconds;
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (TransitionType != NextStateTransitionType.Delay)
            {
                return;
            }

            m_timeBeforeNextState -= deltaTime;
            if (m_timeBeforeNextState <= 0)
            {
                SetState(m_nextState);
            }
        }

        // We authorized for people who know what they do.
        protected void SetNextState(T nextState)
        {
            m_nextState = nextState;
        }
    }
}