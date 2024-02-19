using UnityEngine;

namespace NobunAtelier
{
    public class StateMachineWithUpdate<T, TCollection> : StateMachineComponent<T, TCollection>
        where T : StateDefinition
        where TCollection : DataCollection
    {
        protected virtual void Update()
        {
            Tick(Time.deltaTime);
        }
    }
}