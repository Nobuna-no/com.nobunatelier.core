using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class TriggerBehaviour : MonoBehaviour
    {
        private Collider m_collider;

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
        }

        protected virtual void OnTriggerExit(Collider other)
        {
        }
    }
}