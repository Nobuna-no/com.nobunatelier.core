using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    // TODO: move all animation related info to character or controller logic
    public class SocketStorageBehaviour : MonoBehaviour
    {
        [NaughtyAttributes.InfoBox("IMPORTANT:\n The maximum size of the backpack is determined by the amount of sockets")]
        [SerializeField]
        private Transform[] m_backpackSockets;

        [SerializeField]
        private float m_lerpSpeed = 20;

        [SerializeField]
        private AnimationCurve m_lerpSpeedFactorPerIndex;

        [SerializeField]
        private int m_socketUsageMaxCount = 3;

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

        public void ItemsDropBegin()
        {
            m_isUsable = false;

            foreach (var item in m_backpackQueue)
            {
                item.Drop();
            }
            m_backpackQueue.Clear();

            //if (m_animator && m_seedCountIntName != string.Empty)
            //{
            //    m_animator.SetInteger(m_seedCountIntName, 0);
            //}
        }

        public void ItemsDropEnd()
        {
            m_isUsable = true;
        }

        public void FirstItemDrop()
        {
            if (m_backpackQueue.Count == 0)
            {
                return;
            }

            var item = m_backpackQueue.Dequeue();
            item.Drop();

            //if (m_animator && m_seedCountIntName != string.Empty)
            //{
            //    m_animator.SetInteger(m_seedCountIntName, m_backpackQueue.Count);
            //}
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
                Rigidbody rb = item.GetComponent<Rigidbody>();
                //if (m_useInverseLerping)
                //{
                //    Vector3 targetDes = m_backpackSockets[index].position;
                //    Vector3 currentPos = rb.position;

                //    Vector3 delta = (currentPos - targetDes) * Time.fixedDeltaTime * m_lerpSpeed;
                //    rb.position += delta;
                //}
                //else
                //{
                float indexRatio = (float)index / (float)m_backpackSockets.Length;
                rb.position = Vector3.SlerpUnclamped(rb.position, m_backpackSockets[index].position, Time.fixedDeltaTime * m_lerpSpeed * m_lerpSpeedFactorPerIndex.Evaluate(indexRatio));

                ++index;
            }
        }
    }
}