using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class StateComponent_ExitMachineState<T> : StateComponent<T>
        where T : NobunAtelier.StateDefinition
    {
        public override void Enter()
        {
            base.Enter();
            ParentStateMachine.ExitStateMachine();
        }
    }
}