using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class StateComponent_ExitMachineState<T> : StateComponent<T>
        where T : NobunAtelier.StateDefinition
    {
        private enum ExitStateMachineTrigger
        {
            Manual,
            OnStateEnter,
        }

        [Header("State Machine Exit")]
        [SerializeField]
        private ExitStateMachineTrigger m_trigger = ExitStateMachineTrigger.OnStateEnter;

        public override void Enter()
        {
            base.Enter();
            if (m_trigger == ExitStateMachineTrigger.OnStateEnter)
            {
                ParentStateMachine.ExitStateMachine();
            }
        }

        public void ExitStateMachine()
        {
            ParentStateMachine.ExitStateMachine();
        }
    }
}