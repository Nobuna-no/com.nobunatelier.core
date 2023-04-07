using UnityEngine;

namespace NobunAtelier
{
    public class BaseStateMachine<T> : StateMachineComponent<T>
        where T : StateDefinition
    {
        protected virtual void Update()
        {
            Tick(Time.deltaTime);
        }
    }
}