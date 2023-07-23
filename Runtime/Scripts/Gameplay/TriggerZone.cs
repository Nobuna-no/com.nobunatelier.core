using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier.Gameplay
{
    public class TriggerZone : TriggerBehaviour
    {
        public UnityEvent OnTriggerStart;
        public UnityEvent OnTriggerEnd;

        protected override void OnTriggerEnter(Collider other)
        {
            OnTriggerStart?.Invoke();
        }

        protected override void OnTriggerExit(Collider other)
        {
            OnTriggerEnd?.Invoke();
        }
    }
}