using UnityEngine;

namespace NobunAtelier
{
    public class StateMachineWithFixedUpdate<T, TCollection> : StateMachineComponent<T, TCollection>
        where T : StateDefinition
        where TCollection : DataCollection
    {
        protected virtual void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }
    }
}