using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier.Gameplay
{
    // TODO: move all animation related info to character or controller logic
    public class SocketStorageBehaviour : MonoBehaviour
    {
        [NaughtyAttributes.InfoBox("IMPORTANT:\n The maximum size of the storage is determined by the amount of sockets")]
        [SerializeField]
        private Transform[] m_backpackSockets;

        [SerializeField, Tooltip("Can be use to dynamically change the amount of usable slot.")]
        private int m_socketUsageMaxCount = 3;

        [SerializeField]
        private float m_lerpSpeed = 20;

        [SerializeField]
        private AnimationCurve m_lerpSpeedFactorPerIndex;

        [SerializeField]
        private bool m_useSocketLocalPositionAsOffset = false;

        [SerializeField]
        private bool m_doRotation = false;

        [SerializeField]
        private float m_throwForce = 10;

        [SerializeField] private float m_throwUpwardForce = 1f;

        public int ActiveSocketCount
        {
            get => m_socketUsageMaxCount;
            set
            {
                ItemsDropBegin();
                ItemsDropEnd();
                m_socketUsageMaxCount = value;
            }
        }

        public bool HasAvailableItem => m_backpackQueue.Count > 0;

        public bool HasAvailableSocket => m_isUsable && m_socketUsageMaxCount > m_backpackQueue.Count && m_backpackSockets.Length > m_backpackQueue.Count;

        private Queue<TransportableObjectBehaviour> m_backpackQueue = new Queue<TransportableObjectBehaviour>();
        private bool m_isUsable = true;

        public IReadOnlyList<Transform> Sockets => m_backpackSockets;

        public bool ItemTryPeekFirst(out TransportableObjectBehaviour item)
        {
            item = null;
            if (m_backpackQueue.Count == 0)
            {
                return false;
            }

            item = m_backpackQueue.Peek();
            return item != null;
        }

        public bool ItemTryAdd(TransportableObjectBehaviour item)
        {
            if (!m_isUsable || m_backpackQueue.Count >= m_backpackSockets.Length)
            {
                return false;
            }

            m_backpackQueue.Enqueue(item);

            //if (m_animator && m_seedCountIntName != string.Empty)
            //{
            //    m_animator.SetInteger(m_seedCountIntName, m_backpackQueue.Count);
            //}

            return true;
        }

        public bool ItemTryConsume(out TransportableObjectBehaviour item)
        {
            item = null;
            if (!m_isUsable)
            {
                return false;
            }

            if (m_backpackQueue.TryDequeue(out item))
            {
                //if (m_animator && m_seedCountIntName != string.Empty)
                //{
                //    m_animator.SetInteger(m_seedCountIntName, m_backpackQueue.Count);
                //}
                return true;
            }

            return false;
        }

        [Button]
        public void ItemsDropBegin()
        {
            m_isUsable = false;

            foreach (var item in m_backpackQueue)
            {
                item.Drop(true);
            }
            m_backpackQueue.Clear();
        }

        [Button]
        public void ItemsDropEnd()
        {
            m_isUsable = true;
        }

        [Button]
        public void FirstItemDrop()
        {
            if (m_backpackQueue.Count == 0)
            {
                return;
            }

            var item = m_backpackQueue.Dequeue();
            item.Drop(true);
        }

        [Button]
        public void ThrowFirstItem()
        {
            if (ItemTryConsume(out var item))
            {
                item.Throw((transform.forward + Vector3.up * m_throwUpwardForce).normalized, m_throwForce);
            }
        }

        private void FixedUpdate()
        {
            if (!m_isUsable)
            {
                return;
            }

            int index = 0;
            foreach (var item in m_backpackQueue)
            {
                Rigidbody rb = item.TargetRigidbody;

                float indexRatio = (float)index / (float)m_backpackSockets.Length;
                if (m_useSocketLocalPositionAsOffset)
                {
                    var pos = transform.position + m_backpackSockets[index].localPosition;
                    rb.position = Vector3.SlerpUnclamped(rb.position, pos, Time.fixedDeltaTime * m_lerpSpeed * m_lerpSpeedFactorPerIndex.Evaluate(indexRatio));
                }
                else
                {
                    rb.position = Vector3.SlerpUnclamped(rb.position, m_backpackSockets[index].position, Time.fixedDeltaTime * m_lerpSpeed * m_lerpSpeedFactorPerIndex.Evaluate(indexRatio));
                }

                if (m_doRotation)
                {
                    rb.rotation = Quaternion.Slerp(rb.rotation, transform.rotation, Time.fixedDeltaTime * m_lerpSpeed);
                }
                ++index;
            }
        }
    }
}