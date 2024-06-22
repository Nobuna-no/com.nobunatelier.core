using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class AnimationModule_Event : AnimationModule
    {
        public UnityEvent OnEventRaise;

        public void RaiseEvent() { OnEventRaise?.Invoke(); }
    }
}
