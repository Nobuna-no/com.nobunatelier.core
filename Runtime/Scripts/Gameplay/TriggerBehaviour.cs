using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class TriggerBehaviour : MonoBehaviour
    {
        private Collider m_collider;

        private void Awake()
        {
            m_collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            m_collider.isTrigger = true;
        }

        private void OnDisable()
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