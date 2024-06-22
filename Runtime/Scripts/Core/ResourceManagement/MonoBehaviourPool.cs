using UnityEngine;
using UnityEngine.Pool;

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
        public int ReserveSize => m_initialSize;

        [SerializeField]
        private bool m_createPoolOnAwake;

        [SerializeField]
        protected int m_initialSize = 10;

        [SerializeField]
        private int m_maxSize = 100;

        [SerializeField, Tooltip("Should an exception be thrown if we try to return an existing item, already in the pool?")]
        private bool m_collectionCheck = true;

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
            if (m_createPoolOnAwake)
            {
                ResetPool();
            }
        }

        public void SetInitialSize(int value)
        {
            m_initialSize = value;
        }

        public virtual void ResetPool()
        {
            if (ObjectPool != null)
            {
                ObjectPool.Clear();
                ObjectPool = null;
            }

            ObjectPool = new ObjectPool<T>(OnProductCreation, OnGetFromPool,
                OnProductReleased, OnProductDestruction, m_collectionCheck, m_initialSize, m_maxSize);

            T[] products = new T[m_initialSize];
            for (int i = 0; i < m_initialSize; i++)
            {
                products[i] = ObjectPool.Get();
            }

            for (int i = 0; i < m_initialSize; i++)
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