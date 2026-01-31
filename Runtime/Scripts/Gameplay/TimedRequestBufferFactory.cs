using UnityEngine;
using UnityEngine.Pool;

namespace Physarida
{
    public static class TimedRequestBufferFactory
    {
        private static ObjectPool<TimedRequestBuffer> s_Pool;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (s_Pool != null)
            {
                s_Pool.Clear();
                s_Pool = null;
            }
        }
#endif

        public static TimedRequestBuffer Get(Component owner = null, string name = null)
        {
            EnsurePool();

            TimedRequestBuffer buffer = s_Pool.Get();
            buffer.SetName(name ?? (owner != null ? owner.name : "TimedRequestBuffer"));
            TimedRequestBufferManager.Register(buffer);
            return buffer;
        }

        public static void Release(TimedRequestBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            TimedRequestBufferManager.Unregister(buffer);
            if (s_Pool != null)
            {
                s_Pool.Release(buffer);
            }
        }

        private static void EnsurePool()
        {
            if (s_Pool != null)
            {
                return;
            }

            s_Pool = new ObjectPool<TimedRequestBuffer>(
                createFunc: () => new TimedRequestBuffer(),
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroy,
                collectionCheck: false,
                defaultCapacity: 8,
                maxSize: 128);
        }

        private static void OnGet(TimedRequestBuffer buffer)
        {
        }

        private static void OnRelease(TimedRequestBuffer buffer)
        {
            buffer.Reset();
        }

        private static void OnDestroy(TimedRequestBuffer buffer)
        {
        }
    }
}
