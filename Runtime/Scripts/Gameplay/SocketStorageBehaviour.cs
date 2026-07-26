using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    public enum StoredItemFollowMode
    {
        ParentToSocket,
        SmoothFollow,
    }

    // TODO: move all animation related info to character or controller logic
    public class SocketStorageBehaviour : MonoBehaviour
    {
        [NaughtyAttributes.InfoBox("IMPORTANT:\n The maximum size of the storage is determined by the amount of sockets")]
        [SerializeField]
        private Transform[] m_backpackSockets;

        [SerializeField, Tooltip("Can be use to dynamically change the amount of usable slot.")]
        private int m_socketUsageMaxCount = 3;

        [SerializeField]
        private StoredItemFollowMode m_followMode = StoredItemFollowMode.ParentToSocket;

        [SerializeField, ShowIf(nameof(IsSmoothFollowMode))]
        private float m_lerpSpeed = 20;

        [SerializeField, ShowIf(nameof(IsSmoothFollowMode))]
        private AnimationCurve m_lerpSpeedFactorPerIndex;

        [SerializeField, ShowIf(nameof(IsSmoothFollowMode))]
        private bool m_useSocketLocalPositionAsOffset = false;

        [SerializeField, ShowIf(nameof(IsSmoothFollowMode))]
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
        private readonly Dictionary<TransportableObjectBehaviour, Vector3> m_followPositionVelocities = new Dictionary<TransportableObjectBehaviour, Vector3>();
        private bool m_isUsable = true;

        public IReadOnlyList<Transform> Sockets => m_backpackSockets;

        private bool IsSmoothFollowMode => m_followMode == StoredItemFollowMode.SmoothFollow;

        public bool TryGetActiveSocketTransform(out Transform socket)
        {
            socket = null;
            if (m_backpackQueue.Count == 0 || m_backpackSockets == null || m_backpackSockets.Length == 0)
            {
                return false;
            }

            socket = m_backpackSockets[0];
            return socket != null;
        }

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
            int socketIndex = m_backpackQueue.Count - 1;
            ApplyStoredItemFollow(item, socketIndex, snapImmediate: true);

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
                m_followPositionVelocities.Remove(item);
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
            m_followPositionVelocities.Clear();
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
            m_followPositionVelocities.Remove(item);
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

        private void LateUpdate()
        {
            if (m_followMode != StoredItemFollowMode.SmoothFollow || !m_isUsable || m_backpackQueue.Count == 0)
            {
                return;
            }

            UpdateSmoothFollow(Time.deltaTime);
        }

        private void ApplyStoredItemFollow(TransportableObjectBehaviour item, int socketIndex, bool snapImmediate)
        {
            if (item == null || m_backpackSockets == null || socketIndex < 0 || socketIndex >= m_backpackSockets.Length)
            {
                return;
            }

            Transform socket = m_backpackSockets[socketIndex];
            if (socket == null)
            {
                return;
            }

            if (m_followMode == StoredItemFollowMode.ParentToSocket)
            {
                item.AttachToSocket(socket);
                return;
            }

            if (snapImmediate)
            {
                SnapItemForSmoothFollow(item, socket);
            }
        }

        private void UpdateSmoothFollow(float deltaTime)
        {
            if (m_backpackSockets == null || m_backpackSockets.Length == 0)
            {
                return;
            }

            int index = 0;
            foreach (var item in m_backpackQueue)
            {
                if (item == null)
                {
                    ++index;
                    continue;
                }

                Rigidbody rb = item.TargetRigidbody;
                if (rb == null)
                {
                    ++index;
                    continue;
                }

                Transform socket = m_backpackSockets[index];
                if (socket == null)
                {
                    ++index;
                    continue;
                }

                float indexRatio = (float)index / (float)m_backpackSockets.Length;
                float speedFactor = m_lerpSpeed * m_lerpSpeedFactorPerIndex.Evaluate(indexRatio);
                float smoothTime = speedFactor > 0f ? 1f / speedFactor : 0.01f;

                GetStoredItemTargetWorldPose(item, socket, out Vector3 targetPosition, out Quaternion targetRotation);
                if (!m_followPositionVelocities.TryGetValue(item, out Vector3 positionVelocity))
                {
                    positionVelocity = Vector3.zero;
                }

                Vector3 newPosition = Vector3.SmoothDamp(rb.position, targetPosition, ref positionVelocity, smoothTime, Mathf.Infinity, deltaTime);
                m_followPositionVelocities[item] = positionVelocity;
                rb.MovePosition(newPosition);

                if (m_doRotation)
                {
                    float rotationBlend = 1f - Mathf.Exp(-speedFactor * deltaTime);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationBlend));
                }

                ++index;
            }
        }

        private void GetStoredItemTargetWorldPose(TransportableObjectBehaviour item, Transform socket, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            item.TryGetCarryLocalTransform(out Vector3 localPosition, out Quaternion localRotation);

            if (m_useSocketLocalPositionAsOffset)
            {
                Quaternion baseRotation = m_doRotation ? transform.rotation : socket.rotation;
                worldPosition = GetSocketWorldPosition(socket) + baseRotation * localPosition;
                worldRotation = baseRotation * localRotation;
                return;
            }

            item.GetCarryWorldPose(socket, out worldPosition, out worldRotation);
        }

        private Vector3 GetSocketWorldPosition(Transform socket)
        {
            if (m_useSocketLocalPositionAsOffset)
            {
                return transform.position + socket.localPosition;
            }

            return socket.position;
        }

        private void SnapItemForSmoothFollow(TransportableObjectBehaviour item, Transform socket)
        {
            Rigidbody rb = item.TargetRigidbody;
            if (rb == null)
            {
                return;
            }

            GetStoredItemTargetWorldPose(item, socket, out Vector3 worldPosition, out Quaternion worldRotation);
            rb.position = worldPosition;
            if (m_doRotation)
            {
                rb.rotation = worldRotation;
            }
        }
    }
}
