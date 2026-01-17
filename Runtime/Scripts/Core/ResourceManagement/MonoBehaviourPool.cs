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
        public int ReserveSize => m_InitialSize;

        [SerializeField]
        [FormerlySerializedAs("m_createPoolOnAwake")]
        private bool m_CreatePoolOnAwake;

        [SerializeField]
        [FormerlySerializedAs("m_initialSize")]
        protected int m_InitialSize = 10;

        [SerializeField]
        [FormerlySerializedAs("m_maxSize")]
        private int m_MaxSize = 100;

        [SerializeField, Tooltip("Should an exception be thrown if we try to return an existing item, already in the pool?")]
        [FormerlySerializedAs("m_collectionCheck")]
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

        public void SetInitialSize(int value)
        {
            m_InitialSize = value;
        }

        public virtual void ResetPool()
        {
            if (ObjectPool != null)
            {
                ObjectPool.Clear();
                ObjectPool = null;
            }

            ObjectPool = new ObjectPool<T>(OnProductCreation, OnGetFromPool,
                OnProductReleased, OnProductDestruction, m_CollectionCheck, m_InitialSize, m_MaxSize);

            T[] products = new T[m_InitialSize];
            for (int i = 0; i < m_InitialSize; i++)
            {
                products[i] = ObjectPool.Get();
            }

            for (int i = 0; i < m_InitialSize; i++)
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