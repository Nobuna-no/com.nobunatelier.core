using System;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class TriggerBehaviour : MonoBehaviour
    {
        private Collider m_collider;

        public event Action OnTriggerEnterEvent;
        public event Action OnTriggerExitEvent;

        protected virtual void Awake()
        {
            m_collider = GetComponent<Collider>();
        }

        protected virtual void OnEnable()
        {
            m_collider.isTrigger = true;
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterEvent?.Invoke();
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            OnTriggerExitEvent?.Invoke();
        }
    }
}