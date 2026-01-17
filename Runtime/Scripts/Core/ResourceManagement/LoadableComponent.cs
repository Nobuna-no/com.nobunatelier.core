using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NobunAtelier
{
    /// <summary>
    /// Generic wrapper of Addressable AssetReferenceGameObject for Unity Component.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LoadableComponent<T> : AssetReferenceGameObject
        where T : Component
    {
        public LoadableComponent(string guid) : base(guid)
        { }

        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR
            var gao = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
            if (gao == null)
            {
                return false;
            }

            return gao.GetComponentInChildren<T>();
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Provides product based on a Unity Component type and manages them in a Unity IObjectPool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="AssetRefT"></typeparam>
    public abstract class LoadableComponentPool<T, AssetRefT> : MonoBehaviourPool<T>
        where T : Component
        where AssetRefT : LoadableComponent<T>
    {
        public abstract void SetAssetReference(AssetRefT assetReference);
    }

    /// <summary>
    /// A generic factory for dynamically loading, instantiating, and pooling different types of component instances based on asset references.
    /// Uses Unity's Addressable Asset System and object pooling techniques for efficient asset management.
    /// </summary>
    public abstract class LoadableComponentPoolFactory<T, AssetRefT, PoolT> : LoadableComponentPool<T, AssetRefT>
        where T : Component
        where AssetRefT : LoadableComponent<T>
        where PoolT : LoadableComponentPool<T, AssetRefT>
    {
        private static Dictionary<string, PoolT> s_AddressableFactoriesMap = null;
        private static GameObject s_AtelierFactoryGao;

        [SerializeField]
        private AssetReferenceGameObject m_ObjectPoolPrefab = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            if (s_AtelierFactoryGao != null)
            {
                Object.Destroy(s_AtelierFactoryGao);
                s_AtelierFactoryGao = null;
            }
            
            foreach (var factory in s_AddressableFactoriesMap)
            {
                factory.Value.ObjectPool.Clear();
            }

            s_AddressableFactoriesMap = null;
        }

        private static Dictionary<string, PoolT> Instance
        {
            get
            {
                if (s_AddressableFactoriesMap == null)
                {
                    s_AddressableFactoriesMap = new Dictionary<string, PoolT>();
                    s_AtelierFactoryGao = new GameObject($"Atelier Factory ({typeof(T).Name})");
                }

                return s_AddressableFactoriesMap;
            }
        }

        public static void CreateFactory(AssetRefT assetRef, int initialReserve = 1)
        {
            if (string.IsNullOrEmpty(assetRef.AssetGUID))
            {
                Debug.LogError($"Atelier Factory ({typeof(T).Name}): trying to create a factory with an invalid asset reference.");
                return;
            }

            if (!Instance.ContainsKey(assetRef.AssetGUID))
            {
                Instance.Add(assetRef.AssetGUID, s_AtelierFactoryGao.AddComponent<PoolT>());
                Instance[assetRef.AssetGUID].SetInitialSize(initialReserve);
                Instance[assetRef.AssetGUID].SetAssetReference(assetRef);
                Instance[assetRef.AssetGUID].ResetPool();
            }
        }

        public static T Get(AssetRefT assetRef)
        {
            if (string.IsNullOrEmpty(assetRef.AssetGUID))
            {
                Debug.LogError($"Atelier Factory ({typeof(T).Name}): trying to get a product but asset reference is invalid.");
                return null;
            }

            if (!Instance.ContainsKey(assetRef.AssetGUID))
            {
                CreateFactory(assetRef);
            }

            return Instance[assetRef.AssetGUID].Get();
        }

        public static void Release(AssetRefT assetRef, T product)
        {
            if (!Instance.ContainsKey(assetRef.AssetGUID))
            {
                Debug.LogError($"AddressablePrefabAtelierFactory: Trying to release product '{product.name}' using unknown '{assetRef}'. This may cause a memory leak.");
                return;
            }

            Instance[assetRef.AssetGUID].Release(product);
        }

        // This is going to be used by each individual factory.
        public override void SetAssetReference(AssetRefT assetReference)
        {
            m_ObjectPoolPrefab = assetReference;
        }

        // invoked when creating an item to populate the object pool
        protected override T OnProductCreation()
        {
#if UNITY_EDITOR
            AsyncOperationHandle<GameObject> poolHandle = m_ObjectPoolPrefab.InstantiateAsync(Vector3.zero, Quaternion.identity, transform);
#else
            AsyncOperationHandle<GameObject> poolHandle = objectPoolPrefab.InstantiateAsync(Vector3.zero, Quaternion.identity);
#endif
            poolHandle.WaitForCompletion();
            return poolHandle.Result.GetComponent<T>();
        }

        // invoked when returning an item to the object pool
        protected override void OnProductReleased(T product)
        {
            product.gameObject.SetActive(false);
        }

        // invoked when retrieving the next item from the object pool
        protected override void OnGetFromPool(T product)
        {
            product.gameObject.SetActive(true);
        }

        // invoked when we exceed the maximum number of pooled items (i.e. destroy the pooled object)
        protected override void OnProductDestruction(T product)
        {
            m_ObjectPoolPrefab.ReleaseInstance(product.gameObject);
        }
    }
}