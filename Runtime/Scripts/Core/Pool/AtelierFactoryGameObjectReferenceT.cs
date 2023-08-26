using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NobunAtelier
{
    public class AssetReferenceGameObjectComponentT<T> : AssetReferenceGameObject
    where T : Component
    {
        public AssetReferenceGameObjectComponentT(string guid) : base(guid)
        { }

        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR
            var gao = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
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

    public abstract class AtelierFactoryGameObjectReferenceT<T, AssetRefT> : AtelierFactoryT<T>
        where T : Component
        where AssetRefT : AssetReferenceGameObjectComponentT<T>
    {
        public abstract void SetAssetReference(AssetRefT assetReference);
    }

    public abstract class AtelierFactoryGameObjectReferenceT<T, AssetRefT, AtelierFactoryT> : AtelierFactoryGameObjectReferenceT<T, AssetRefT>
        where T : Component
        where AssetRefT : AssetReferenceGameObjectComponentT<T>
        where AtelierFactoryT : AtelierFactoryGameObjectReferenceT<T, AssetRefT>
    {
        private static Dictionary<string, AtelierFactoryT> s_addressableFactoriesMap;
        private static GameObject s_atelierFactoryGao;

        [SerializeField]
        private AssetReferenceGameObject objectPoolPrefab = null;

        private static Dictionary<string, AtelierFactoryT> Instance
        {
            get
            {
                if (s_addressableFactoriesMap == null)
                {
                    s_addressableFactoriesMap = new Dictionary<string, AtelierFactoryT>();
                    s_atelierFactoryGao = new GameObject($"Atelier Factory ({typeof(T).Name})");
                }

                return s_addressableFactoriesMap;
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
                Instance.Add(assetRef.AssetGUID, s_atelierFactoryGao.AddComponent<AtelierFactoryT>());
                Instance[assetRef.AssetGUID].SetInitialSize(initialReserve);
                Instance[assetRef.AssetGUID].SetAssetReference(assetRef);
                Instance[assetRef.AssetGUID].ResetPool();
            }
        }

        public static T GetProduct(AssetRefT assetRef)
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

            return Instance[assetRef.AssetGUID].GetProduct();
        }

        public static void ReleaseProduct(AssetRefT assetRef, T product)
        {
            if (!Instance.ContainsKey(assetRef.AssetGUID))
            {
                Debug.LogError($"AddressablePrefabAtelierFactory: Trying to release product '{product.name}' using unknown '{assetRef}'. This may cause a memory leak.");
                return;
            }

            Instance[assetRef.AssetGUID].ReleaseProduct(product);
        }

        public override void SetAssetReference(AssetRefT assetReference)
        {
            objectPoolPrefab = assetReference;
        }

        //public override void ResetPool()
        //{
        //    base.ResetPool();

        //    // Force to create a first object as the first instantiate async can take a while.
        //    ObjectPool.Get(out T obj);
        //    ObjectPool.Release(obj);
        //}

        // invoked when creating an item to populate the object pool
        protected override T OnProductCreation()
        {
            AsyncOperationHandle<GameObject> poolHandle = objectPoolPrefab.InstantiateAsync(Vector3.zero, Quaternion.identity);
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
            objectPoolPrefab.ReleaseInstance(product.gameObject);
        }
    }
}