using System.Collections.Generic;
using NobunAtelier;
using UnityEngine;

namespace Physarida
{
    public sealed class TimedRequestBufferManager : MonoBehaviourService<TimedRequestBufferManager>
    {
        private readonly List<TimedRequestBuffer> m_Buffers = new List<TimedRequestBuffer>();

        private void OnEnable()
        {
            m_Buffers.Clear();
        }

        public static void Register(TimedRequestBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            TimedRequestBufferManager driver = EnsureInstance();
            driver.RegisterInternal(buffer);
        }

        public static void Unregister(TimedRequestBuffer buffer)
        {
            if (!IsSingletonValid || buffer == null)
            {
                return;
            }

            Instance.UnregisterInternal(buffer);
        }

        private static TimedRequestBufferManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var singleton = new GameObject("[ TimedRequestBufferDriver ]").AddComponent<TimedRequestBufferManager>();
            return singleton;
        }

        private void Update()
        {
            if (m_Buffers.Count == 0)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float unscaledDeltaTime = Time.unscaledDeltaTime;

            for (int i = 0; i < m_Buffers.Count; i++)
            {
                m_Buffers[i].Tick(deltaTime, unscaledDeltaTime);
            }
        }

        private void RegisterInternal(TimedRequestBuffer buffer)
        {
            if (m_Buffers.Contains(buffer))
            {
                return;
            }

            m_Buffers.Add(buffer);
        }

        private void UnregisterInternal(TimedRequestBuffer buffer)
        {
            m_Buffers.Remove(buffer);
        }

#if UNITY_EDITOR
        public void GetBuffers(List<TimedRequestBuffer> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            results.AddRange(m_Buffers);
        }
#endif
    }
}
