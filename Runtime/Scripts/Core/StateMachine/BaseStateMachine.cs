using UnityEngine;

namespace NobunAtelier
{
    public class BaseStateMachine<T> : StateMachineComponent<T>
        where T : StateDefinition
    {
        protected virtual void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }
    }
}