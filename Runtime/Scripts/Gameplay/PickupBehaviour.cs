using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    public class PickupBehaviour : TriggerBehaviour
    {
        public List<TransportableObjectBehaviour> GatherableObjects => m_baseGatherableObjects;
        private List<TransportableObjectBehaviour> m_baseGatherableObjects = new List<TransportableObjectBehaviour>();

        public event System.Action OnGatherableObjectAdded;

        public event System.Action OnGatherableObjectRemoved;

        public bool GatherTry(out TransportableObjectBehaviour obj)
        {
            obj = null;

            if (m_baseGatherableObjects.Count == 0)
            {
                return false;
            }

            obj = m_baseGatherableObjects[m_baseGatherableObjects.Count - 1];
            m_baseGatherableObjects.Remove(obj);

            return obj.Pick();
        }

        protected override void OnTriggerEnter(Collider other)
        {
            TransportableObjectBehaviour gatherable = other.GetComponent<TransportableObjectBehaviour>();
            if (gatherable != null && !m_baseGatherableObjects.Contains(gatherable))
            {
                m_baseGatherableObjects.Add(gatherable);
                OnGatherableObjectAdded?.Invoke();
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            TransportableObjectBehaviour gatherable = other.GetComponent<TransportableObjectBehaviour>();
            if (gatherable != null && m_baseGatherableObjects.Contains(gatherable))
            {
                m_baseGatherableObjects.Remove(gatherable);
                OnGatherableObjectRemoved?.Invoke();
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                foreach (var m in m_baseGatherableObjects)
                {
                    Gizmos.DrawWireCube(m.transform.position, Vector3.one);
                }
            }
        }
    }
}