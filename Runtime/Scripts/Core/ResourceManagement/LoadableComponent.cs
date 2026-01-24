using System.Collections.Generic;
#if UNITY_EDITOR
using System;
using System.Reflection;
#endif
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

#if UNITY_EDITOR
    /// <summary>
    /// This class is used to reset all pool factories on application startup.
    /// </summary>
    public static class PoolFactoryRuntimeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetAllFactories()
        {
            ResetGenericFactories();
        }

        static void ResetGenericFactories()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var baseType = type.BaseType;
                    if (baseType == null)
                        continue;

                    if (!baseType.IsGenericType)
                        continue;

                    if (baseType.ContainsGenericParameters)
                        continue;

                    if (baseType.GetGenericTypeDefinition() != typeof(LoadableComponentPoolFactory<,,>))
                        continue;

                    var resetMethod = baseType.GetMethod(
                        "RuntimeReset",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    resetMethod?.Invoke(null, null);
                }
            }
        }
    }
#endif

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
        private static Dictionary<string, PoolT> s_AddressableFactoryMap = null;
        private static GameObject s_FactoryGao;
        private static string k_FactoryName = typeof(LoadableComponentPoolFactory<T, AssetRefT, PoolT>).Name;

        [SerializeField]
        private AssetReferenceGameObject objectPoolPrefab = null;

        private static Dictionary<string, PoolT> Instance
        {
            get
            {
                if (s_AddressableFactoryMap == null)
                {
                    s_AddressableFactoryMap = new Dictionary<string, PoolT>();
                    s_FactoryGao = new GameObject($"Atelier Factory ({typeof(T).Name})");
                }

                return s_AddressableFactoryMap;
            }
        }

        private static void RuntimeReset()
        {
            // Debug.Log($"Resetting pool factory {typeof(T)}");

            if (s_FactoryGao != null)
            {
                UnityEngine.Object.DestroyImmediate(s_FactoryGao);
                s_FactoryGao = null;
            }

            s_AddressableFactoryMap?.Clear();
            s_AddressableFactoryMap = null;

            Instance?.Clear();
        }

        public static void CreateFactory(AssetRefT assetRef, int initialReserve = 3)
        {
            if (string.IsNullOrEmpty(assetRef.AssetGUID))
            {
                Debug.LogError($"{k_FactoryName}: trying to create a factory with an invalid asset reference.");
                return;
            }

            if (!Instance.ContainsKey(assetRef.AssetGUID) || Instance[assetRef.AssetGUID] == null)
            {
                Instance[assetRef.AssetGUID] = s_FactoryGao.AddComponent<PoolT>();
            }

            Instance[assetRef.AssetGUID].SetAssetReference(assetRef);
            Instance[assetRef.AssetGUID].ResetPool(initialReserve);
        }

        public static T Get(AssetRefT assetRef)
        {
            if (string.IsNullOrEmpty(assetRef.AssetGUID))
            {
                Debug.LogError($"{k_FactoryName}: trying to get a product but asset reference is invalid.");
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
            if (assetRef == null)
            {
                Debug.LogError($"{k_FactoryName}: Trying to release product '{product.name}' using unknown asset reference.");
                return;
            }

            if (!Instance.ContainsKey(assetRef.AssetGUID))
            {
                Debug.LogError($"{k_FactoryName}: Trying to release product '{product.name}' using unknown '{assetRef}'. This may cause a memory leak.");
                return;
            }

            Instance[assetRef.AssetGUID].Release(product);
        }

        // This is going to be used by each individual factory.
        public override void SetAssetReference(AssetRefT assetReference)
        {
            objectPoolPrefab = assetReference;
        }

        // invoked when creating an item to populate the object pool
        protected override T OnProductCreation()
        {
#if UNITY_EDITOR
            AsyncOperationHandle<GameObject> poolHandle = objectPoolPrefab.InstantiateAsync(Vector3.zero, Quaternion.identity, transform);
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
            if (product != null)
            {
                objectPoolPrefab.ReleaseInstance(product.gameObject);
            }
        }
    }
}