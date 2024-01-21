using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    public class PickupBehaviour : TriggerBehaviour
    {
        [SerializeField] private SocketStorageBehaviour m_storageComponent;
        public List<TransportableObjectBehaviour> GatherableObjects => m_baseGatherableObjects;
        private List<TransportableObjectBehaviour> m_baseGatherableObjects = new List<TransportableObjectBehaviour>();
        protected SocketStorageBehaviour StorageComponent => m_storageComponent;

        public event System.Action<TransportableObjectBehaviour> OnGatherableObjectAdded;

        public event System.Action<TransportableObjectBehaviour> OnGatherableObjectRemoved;

        public bool CanPickup => enabled && !m_isPicking && m_storageComponent && m_storageComponent.HasAvailableSocket;
        private bool m_isPicking = false;

        [Button]
        public void TryGather()
        {
            if (!CanPickup)
            {
                return;
            }

            if (GatherTry(out var obj))
            {
                m_storageComponent.ItemTryAdd(obj);
            }
        }

        public bool GatherTry(out TransportableObjectBehaviour obj)
        {
            if (!enabled)
            {
                obj = null;
                return false;
            }

            obj = null;

            if (m_baseGatherableObjects.Count == 0)
            {
                return false;
            }

            obj = m_baseGatherableObjects[m_baseGatherableObjects.Count - 1];
            m_baseGatherableObjects.Remove(obj);

            // Used to help listener that depends on CanPickup.
            m_isPicking = true;
            OnGatherableObjectRemoved?.Invoke(obj);
            m_isPicking = false;
            return obj.Pick();
        }

        protected override void OnTriggerEnter(Collider other)
        {
            TransportableObjectBehaviour gatherable = other.GetComponent<TransportableObjectBehaviour>();
            if (gatherable != null && !m_baseGatherableObjects.Contains(gatherable))
            {
                m_baseGatherableObjects.Add(gatherable);
                OnGatherableObjectAdded?.Invoke(gatherable);
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            TransportableObjectBehaviour gatherable = other.GetComponent<TransportableObjectBehaviour>();
            if (gatherable != null && m_baseGatherableObjects.Contains(gatherable))
            {
                m_baseGatherableObjects.Remove(gatherable);
                OnGatherableObjectRemoved?.Invoke(gatherable);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (var g in m_baseGatherableObjects)
            {
                OnGatherableObjectAdded?.Invoke(g);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            foreach (var g in m_baseGatherableObjects)
            {
                OnGatherableObjectRemoved?.Invoke(g);
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