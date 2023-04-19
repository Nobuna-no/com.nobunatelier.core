using UnityEngine;
using UnityEngine.Pool;

namespace NobunAtelier
{
    public abstract class AtelierFactory<T> : MonoBehaviour
        where T : class
    {
        public int ReserveSize => m_initialSize;

        [SerializeField]
        protected int m_initialSize = 10;

        [SerializeField]
        private int m_maxSize = 100;

        [SerializeField, Tooltip("Should an exception be thrown if we try to return an existing item, already in the pool?")]
        private bool m_collectionCheck = true;

        public IObjectPool<T> ObjectPool { get; protected set; } = null;

        public virtual T GetProduct()
        {
            return ObjectPool.Get();
        }

        public virtual void ReleaseProduct(T obj)
        {
            ObjectPool.Release(obj);
        }

        private void Awake()
        {
            ObjectPool = new ObjectPool<T>(OnProductCreation, OnGetFromPool,
                OnProductReleased, OnProductDestruction, m_collectionCheck, m_initialSize, m_maxSize);
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