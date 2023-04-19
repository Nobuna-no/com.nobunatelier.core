using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NobunAtelier
{
    public class AddressablePrefabAtelierFactory<T> : AtelierFactory<T> where T : MonoBehaviour
    {
        [SerializeField]
        private AssetReference objectPoolPrefab = null;

        // invoked when creating an item to populate the object pool
        protected override T OnProductCreation()
        {
            var poolHandle = objectPoolPrefab.InstantiateAsync(Vector3.zero, Quaternion.identity);
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
