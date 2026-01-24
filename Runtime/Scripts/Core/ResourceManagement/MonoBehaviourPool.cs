using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public interface IPool<T>
    {
        T Get();
        void GetAsync(System.Action<T> onCompleted);
        void Release(T instance);
    }

    /// <summary>
    /// Provides product based on type (factory) and manages them in a Unity IObjectPool.
    /// </summary>
    public abstract class MonoBehaviourPool<T> : MonoBehaviour, IPool<T>
        where T : class
    {
        /// <summary>
        /// The initial size of the pool.
        /// </summary>
        public int ReserveSize => m_DefaultReserveSize;

        [FormerlySerializedAs("m_createPoolOnAwake")]
        [SerializeField]
        private bool m_CreatePoolOnAwake;

        [FormerlySerializedAs("m_initialSize")]
        [SerializeField]
        protected int m_DefaultReserveSize = 10;

        [FormerlySerializedAs("m_maxSize")]
        [SerializeField]
        private int m_MaxSize = 100;

        [FormerlySerializedAs("m_collectionCheck")]
        [SerializeField, Tooltip("Should an exception be thrown if we try to return an existing item, already in the pool?")]
        private bool m_CollectionCheck = true;

        public IObjectPool<T> ObjectPool { get; protected set; } = null;

        public virtual T Get()
        {
#if DEBUG
            if (ObjectPool == null)
            {
                Debug.LogError("Object pool is not initialized.");
                return default(T);
            }
#endif
            return ObjectPool.Get();
        }

        public virtual void GetAsync(System.Action<T> onCompleted)
        {
#if DEBUG
            if (ObjectPool == null)
            {
                Debug.LogError("Object pool is not initialized.");
                return;
            }
#endif
            onCompleted?.Invoke(ObjectPool.Get());
        }

        public virtual void Release(T obj)
        {
            ObjectPool.Release(obj);
        }

        private void Awake()
        {
            if (m_CreatePoolOnAwake)
            {
                ResetPool();
            }
        }

        /// <summary>
        /// Reset the pool. If the pool is not allocated, it will be allocated. If the pool is allocated, it will be cleared.
        /// </summary>
        /// <param name="reserveSize">The size of the pool. If less than or equal to 0, the default reserve size will be used.</param>
        public virtual void ResetPool(int reserveSize = -1)
        {
            ObjectPool?.Clear();

            int reserveSizeToUse = reserveSize <= 0 ? m_DefaultReserveSize : Mathf.Max(1, m_DefaultReserveSize);

            if (ObjectPool == null)
            {
                ObjectPool = new ObjectPool<T>(OnProductCreation, OnGetFromPool,
                    OnProductReleased, OnProductDestruction, m_CollectionCheck, reserveSizeToUse, m_MaxSize);
            }

            T[] products = new T[reserveSizeToUse];
            for (int i = 0; i < reserveSizeToUse; i++)
            {
                products[i] = ObjectPool.Get();
            }

            for (int i = 0; i < reserveSizeToUse; i++)
            {
                ObjectPool.Release(products[i]);
            }
        }

        // invoked when creating an item to populate the object pool
        protected abstract T OnProductCreation();

        // invoked when returning an item to the object pool
        protected abstract void OnProductReleased(T product);

        // invoked when retrieving the next item from the object pool
        protected abstract void OnGetFromPool(T product);

        // invoked when we exceed the maximum number of pooled items (i.e. destroy the pooled object)
        protected abstract void OnProductDestruction(T product);
    }
}