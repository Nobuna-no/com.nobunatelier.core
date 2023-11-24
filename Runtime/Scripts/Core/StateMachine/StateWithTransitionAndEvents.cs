using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class StateWithTransitionAndEvents<T, TCollection> : StateWithTransition<T, TCollection>
        where T : NobunAtelier.StateDefinition
        where TCollection : DataCollection
    {
        // To improve by creating an intermediate that have only UnityEvent and one with timed transition
        [Header("Base State Events")]
        [Header("Events")]
        public UnityEvent OnStateEnter;

        public UnityEvent OnStateExit;

        public override void Enter()
        {
            OnStateEnter?.Invoke();
            base.Enter();
        }

        public override void Exit()
        {
            OnStateExit?.Invoke();
            base.Exit();
        }
    }
}