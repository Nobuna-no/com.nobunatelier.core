using System;
using System.Collections.Generic;
using UnityEngine;

namespace Physarida
{
    public sealed class TimedRequestHandle
    {
        internal TimedRequestBuffer Buffer { get; }
        internal float BufferDuration { get; private set; }
        internal float RemainingTime { get; private set; }
        internal bool HasRequest { get; private set; }
        internal bool UseUnscaledTime { get; private set; }
        internal Func<bool> CanConsume { get; private set; }
        internal Action OnConsume { get; private set; }
        internal Component Owner { get; private set; }
        internal bool HasOwner { get; private set; }
#if UNITY_EDITOR
        internal string DebugLabel { get; private set; }
#endif

        internal TimedRequestHandle(TimedRequestBuffer buffer)
        {
            Buffer = buffer;
        }

        public void Request()
        {
            Buffer?.RequestInternal(this);
        }

        public void Unregister()
        {
            Buffer?.UnregisterInternal(this);
        }

        public void UpdateBufferDuration(float bufferDuration)
        {
            Buffer?.UpdateBufferDurationInternal(this, bufferDuration);
        }

        public void Clear()
        {
            ClearInternal();
        }

        internal void Configure(float bufferDuration, bool useUnscaledTime, Func<bool> canConsume, Action onConsume,
            Component owner, string debugLabel)
        {
            BufferDuration = Mathf.Max(0f, bufferDuration);
            UseUnscaledTime = useUnscaledTime;
            CanConsume = canConsume;
            OnConsume = onConsume;
            Owner = owner;
            HasOwner = owner != null;
#if UNITY_EDITOR
            DebugLabel = debugLabel;
#endif
        }

        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (!HasRequest)
            {
                return;
            }

            float delta = UseUnscaledTime ? unscaledDeltaTime : deltaTime;
            RemainingTime -= delta;
            if (RemainingTime <= 0f)
            {
                HasRequest = false;
            }
        }

        internal bool TryConsume()
        {
            if (!HasRequest)
            {
                return false;
            }

            if (CanConsume != null && !CanConsume())
            {
                return false;
            }

            HasRequest = false;
            OnConsume?.Invoke();
            return true;
        }

        internal void RequestInternal()
        {
            HasRequest = true;
            RemainingTime = BufferDuration;
        }

        internal void ClearInternal()
        {
            HasRequest = false;
            RemainingTime = 0f;
        }
    }

    public sealed class TimedRequestBuffer
    {
#if UNITY_EDITOR
        public enum HistoryEventType
        {
            Register,
            Request,
            Consume,
            Expire,
            Unregister,
            Clear,
            UpdateDuration
        }

        public readonly struct HistoryEntry
        {
            public readonly HistoryEventType EventType;
            public readonly string DebugLabel;
            public readonly int Frame;
            public readonly float Time;
            public readonly float BufferDuration;
            public readonly float RemainingTime;
            public readonly string OwnerName;
            public readonly string ConsumeMethod;

            public HistoryEntry(HistoryEventType eventType, string debugLabel, int frame, float time, float bufferDuration,
                float remainingTime, string ownerName, string consumeMethod)
            {
                EventType = eventType;
                DebugLabel = debugLabel;
                Frame = frame;
                Time = time;
                BufferDuration = bufferDuration;
                RemainingTime = remainingTime;
                OwnerName = ownerName;
                ConsumeMethod = consumeMethod;
            }
        }

        public readonly struct DebugInfo
        {
            public readonly string DebugLabel;
            public readonly float BufferDuration;
            public readonly float RemainingTime;
            public readonly bool HasRequest;
            public readonly bool UseUnscaledTime;
            public readonly bool HasOwner;
            public readonly string OwnerName;
            public readonly string ConsumeMethod;

            public DebugInfo(string debugLabel, float bufferDuration, float remainingTime, bool hasRequest,
                bool useUnscaledTime, bool hasOwner, string ownerName, string consumeMethod)
            {
                DebugLabel = debugLabel;
                BufferDuration = bufferDuration;
                RemainingTime = remainingTime;
                HasRequest = hasRequest;
                UseUnscaledTime = useUnscaledTime;
                HasOwner = hasOwner;
                OwnerName = ownerName;
                ConsumeMethod = consumeMethod;
            }
        }
#endif

        public sealed class Builder
        {
            private readonly TimedRequestBuffer m_Buffer;
            private readonly TimedRequestHandle m_Handle;
            private float m_BufferDuration;
            private bool m_UseUnscaledTime;
            private Func<bool> m_CanConsume;
            private Action m_OnConsume;
            private Component m_Owner;
            private string m_DebugLabel;

            internal Builder(TimedRequestBuffer buffer, TimedRequestHandle handle)
            {
                m_Buffer = buffer;
                m_Handle = handle;
            }

            public Builder WithBufferDuration(float seconds)
            {
                m_BufferDuration = seconds;
                return this;
            }

            public Builder UseUnscaledTime(bool useUnscaledTime)
            {
                m_UseUnscaledTime = useUnscaledTime;
                return this;
            }

            public Builder When(Func<bool> condition)
            {
                m_CanConsume = condition;
                return this;
            }

            public Builder OnConsume(Action onConsume)
            {
                m_OnConsume = onConsume;
                return this;
            }

            public Builder OwnedBy(Component owner)
            {
                m_Owner = owner;
                return this;
            }

            public Builder WithDebugLabel(string label)
            {
                m_DebugLabel = label;
                return this;
            }

            public TimedRequestHandle Build()
            {
                m_Buffer.RegisterOrUpdate(m_Handle, m_BufferDuration, m_UseUnscaledTime, m_CanConsume, m_OnConsume, m_Owner,
                    m_DebugLabel);
                return m_Handle;
            }
        }

        private readonly List<TimedRequestHandle> m_Requests = new List<TimedRequestHandle>();
        private readonly List<TimedRequestHandle> m_RemoveBuffer = new List<TimedRequestHandle>();
#if UNITY_EDITOR
        private readonly List<HistoryEntry> m_History = new List<HistoryEntry>();
        private int m_HistoryCapacity = 64;
        private bool m_HistoryEnabled = true;
#endif
        private string m_Name;

        public string Name => m_Name;

        public Builder Register()
        {
            var handle = new TimedRequestHandle(this);
            return new Builder(this, handle);
        }

        public void SetName(string name)
        {
            m_Name = name;
        }

#if UNITY_EDITOR
        public void SetHistoryCapacity(int capacity)
        {
            m_HistoryCapacity = Mathf.Max(0, capacity);
            if (m_HistoryCapacity == 0)
            {
                m_History.Clear();
            }
            else if (m_History.Count > m_HistoryCapacity)
            {
                int removeCount = m_History.Count - m_HistoryCapacity;
                m_History.RemoveRange(0, removeCount);
            }
        }

        public void EnableHistory(bool enabled)
        {
            m_HistoryEnabled = enabled;
            if (!enabled)
            {
                m_History.Clear();
            }
        }

        public void GetDebugInfo(List<DebugInfo> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            for (int i = 0; i < m_Requests.Count; i++)
            {
                TimedRequestHandle request = m_Requests[i];
                string ownerName = request.HasOwner
                    ? request.Owner != null ? request.Owner.name : "<Missing>"
                    : "<None>";
                string consumeMethod = request.OnConsume != null && request.OnConsume.Method != null
                    ? $"{request.OnConsume.Method.DeclaringType?.Name}.{request.OnConsume.Method.Name}"
                    : "<None>";
                string label = string.IsNullOrWhiteSpace(request.DebugLabel) ? "<Unlabeled>" : request.DebugLabel;

                results.Add(new DebugInfo(
                    label,
                    request.BufferDuration,
                    request.RemainingTime,
                    request.HasRequest,
                    request.UseUnscaledTime,
                    request.HasOwner,
                    ownerName,
                    consumeMethod));
            }
        }

        public void GetHistory(List<HistoryEntry> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            results.AddRange(m_History);
        }
#endif

        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            for (int i = 0; i < m_Requests.Count; i++)
            {
                TimedRequestHandle request = m_Requests[i];

                if (request.HasOwner && request.Owner == null)
                {
                    m_RemoveBuffer.Add(request);
                    continue;
                }

                bool hadRequest = request.HasRequest;
                float beforeRemaining = request.RemainingTime;
                request.Tick(deltaTime, unscaledDeltaTime);
#if UNITY_EDITOR
                if (hadRequest && !request.HasRequest && beforeRemaining > 0f)
                {
                    AddHistory(HistoryEventType.Expire, request);
                }
#endif

                if (request.TryConsume())
                {
#if UNITY_EDITOR
                    AddHistory(HistoryEventType.Consume, request);
#endif
                }
            }

            if (m_RemoveBuffer.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < m_RemoveBuffer.Count; i++)
            {
                UnregisterInternal(m_RemoveBuffer[i]);
            }

            m_RemoveBuffer.Clear();
        }

        internal void Reset()
        {
            m_Requests.Clear();
            m_RemoveBuffer.Clear();
#if UNITY_EDITOR
            m_History.Clear();
            m_HistoryCapacity = 64;
            m_HistoryEnabled = true;
#endif
            m_Name = null;
        }

        internal void RegisterOrUpdate(TimedRequestHandle handle, float bufferDuration, bool useUnscaledTime, Func<bool> canConsume,
            Action onConsume, Component owner, string debugLabel)
        {
            if (handle == null)
            {
                return;
            }

            if (!m_Requests.Contains(handle))
            {
                m_Requests.Add(handle);
#if UNITY_EDITOR
                AddHistory(HistoryEventType.Register, handle);
#endif
            }

            handle.Configure(bufferDuration, useUnscaledTime, canConsume, onConsume, owner, debugLabel);
        }

        internal void RequestInternal(TimedRequestHandle handle)
        {
            if (handle == null || !m_Requests.Contains(handle))
            {
                Debug.LogWarning("TimedRequestBuffer request ignored, handle not registered.");
                return;
            }

            handle.RequestInternal();
#if UNITY_EDITOR
            AddHistory(HistoryEventType.Request, handle);
#endif
            if (handle.TryConsume())
            {
#if UNITY_EDITOR
                AddHistory(HistoryEventType.Consume, handle);
#endif
            }
        }

        internal void UnregisterInternal(TimedRequestHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (!m_Requests.Remove(handle))
            {
                return;
            }

#if UNITY_EDITOR
            AddHistory(HistoryEventType.Unregister, handle);
#endif
        }

        internal void UpdateBufferDurationInternal(TimedRequestHandle handle, float bufferDuration)
        {
            if (handle == null || !m_Requests.Contains(handle))
            {
                return;
            }

            string debugLabel = null;
#if UNITY_EDITOR
            debugLabel = handle.DebugLabel;
#endif
            handle.Configure(bufferDuration, handle.UseUnscaledTime, handle.CanConsume, handle.OnConsume, handle.Owner,
                debugLabel);
#if UNITY_EDITOR
            AddHistory(HistoryEventType.UpdateDuration, handle);
#endif
        }

#if UNITY_EDITOR
        private void AddHistory(HistoryEventType eventType, TimedRequestHandle request)
        {
            if (!m_HistoryEnabled || m_HistoryCapacity == 0)
            {
                return;
            }

            string ownerName = request.HasOwner
                ? request.Owner != null ? request.Owner.name : "<Missing>"
                : "<None>";
            string consumeMethod = request.OnConsume != null && request.OnConsume.Method != null
                ? $"{request.OnConsume.Method.DeclaringType?.Name}.{request.OnConsume.Method.Name}"
                : "<None>";
            string label = string.IsNullOrWhiteSpace(request.DebugLabel) ? "<Unlabeled>" : request.DebugLabel;

            m_History.Add(new HistoryEntry(
                eventType,
                label,
                Time.frameCount,
                Time.time,
                request.BufferDuration,
                request.RemainingTime,
                ownerName,
                consumeMethod));

            if (m_History.Count > m_HistoryCapacity)
            {
                int removeCount = m_History.Count - m_HistoryCapacity;
                m_History.RemoveRange(0, removeCount);
            }
        }
#endif
    }
}
