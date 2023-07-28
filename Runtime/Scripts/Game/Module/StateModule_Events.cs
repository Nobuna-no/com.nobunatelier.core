using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Events")]
    public class StateModule_Events : StateComponentModule
    {
        public UnityEvent OnStateEnter;
        public UnityEvent OnStateExit;

        public override void Enter()
        {
            OnStateEnter?.Invoke();
        }

        public override void Exit()
        {
            OnStateExit?.Invoke();
        }
    }
}