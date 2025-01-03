using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// This is a pool that can instantiate PoolableBehaviour contains in the register PoolObjectDefinition.
    /// As each poolable object can define it's own creation and spawned implementation,
    /// it's possible to use a single PoolManager to handle any object type.
    /// There is also no need to specialized the manager for the spawning method.
    /// This is a dynamic pool but is not yet optimized.
    /// </summary>
    public sealed class DataDrivenFactoryManager : SingletonMonoBehaviour<DataDrivenFactoryManager>
    {
        [System.Flags]
        public enum PoolBehaviour
        {
            // Is the pool manager allowed to instantiate new object if there is no available object.
            AllowsLazyInstancing = 1 << 1,

            WarmsLazyInstancing = 1 << 2,
            FillsPoolOnStart = 1 << 3,
            LogDebug = 0x1 << 7,
        }

        [Header("Data-Driven Pool Factory")]
        [Tooltip("Parent transform. Set to null in release build for optimization.")]
        [SerializeField] private Transform m_reserveParent = null;
        [Tooltip("Default spawn position offset. Useful to avoid spawning things at 0,0,0.")]
        [SerializeField] private Vector3 m_SpawnOffset = Vector3.zero;

        [SerializeField]
        private PoolBehaviour m_behaviour =
                PoolBehaviour.AllowsLazyInstancing
                | PoolBehaviour.WarmsLazyInstancing
                | PoolBehaviour.FillsPoolOnStart;

        // This is probably the biggest flaw of this design.
        // Custom DataDefinition<PoolObjectDefinition> cannot be register as collection.
        [SerializeField] private FactoryProductCollection[] m_initialCollections = null;

        // This can also be use by custom PoolObjectDefinition that can't be assigned to the collection.
        [SerializeField] private FactoryProductDefinition[] m_initialDefinitions = null;

        private Dictionary<FactoryProductDefinition, List<FactoryProduct>> m_productPerID = new Dictionary<FactoryProductDefinition, List<FactoryProduct>>();

        public static void Register(FactoryProductDefinition definition)
        {
            Instance.RegisterDefinition(definition);
        }

        public static void Register<TCollection, TDefinition>(TCollection collection)
            where TCollection : DataCollection<TDefinition>
            where TDefinition : FactoryProductDefinition
        {
            Instance.RegisterCollection<TCollection, TDefinition>(collection);
        }

        public static void Unregister(FactoryProductDefinition definition)
        {
            Instance.UnregisterDefinition(definition);
        }

        public static void Unregister<TCollection, TDefinition>(TCollection collection)
            where TCollection : DataCollection<TDefinition>
            where TDefinition : FactoryProductDefinition
        {
            Instance.UnregisterCollection<TCollection, TDefinition>(collection);
        }

        public static FactoryProduct Get(FactoryProductDefinition definition)
        {
            return Instance.Get_Internal(definition);
        }

        public static ComponentT Get<ComponentT>(FactoryProductDefinition definition)
            where ComponentT : Component
        {
            return Instance.Get_Internal<ComponentT>(definition);
        }

        public static IReadOnlyList<FactoryProduct> Get(FactoryProductDefinition definition, int count)
        {
            return Instance.Get_Internal(definition, count);
        }

        public static IReadOnlyList<ComponentT> Get<ComponentT>(FactoryProductDefinition definition, int count)
            where ComponentT : Component
        {
            return Instance.Get_Internal<ComponentT>(definition, count);
        }

        public static void GetAsync(FactoryProductDefinition definition, System.Action<FactoryProduct> onCompleted)
        {
            Instance.GetAsync_Internal(definition, onCompleted);
        }

        public static void GetAsync<ComponentT>(FactoryProductDefinition definition, System.Action<ComponentT> onCompleted)
            where ComponentT : Component
        {
            Instance.GetAsync_Internal<ComponentT>(definition, onCompleted);
        }

        public static void GetAsync(FactoryProductDefinition definition, int count, System.Action<IReadOnlyList<FactoryProduct>> onCompleted)
        {
            Instance.GetAsync_Internal(definition, count, onCompleted);
        }

        public static void GetAsync<ComponentT>(FactoryProductDefinition definition, int count, System.Action<IReadOnlyList<ComponentT>> onCompleted)
            where ComponentT : Component
        {
            Instance.GetAsync_Internal<ComponentT>(definition, count, onCompleted);
        }

        public static void Release(FactoryProduct instance)
        {
            Instance.Release_Internal(instance);
        }

        private void RegisterCollection<TCollection, TDefinition>(TCollection collection)
            where TCollection : DataCollection<TDefinition>
            where TDefinition : FactoryProductDefinition
        {
            Debug.Assert(collection, this);

            FillReservesInBackbground(collection.Definitions);
        }

        private void UnregisterCollection<TCollection, TDefinition>(TCollection collection)
            where TCollection : DataCollection<TDefinition>
            where TDefinition : FactoryProductDefinition
        {
            Debug.Assert(collection, this);

            foreach (var definition in collection.Definitions)
            {
                ClearReserve(definition);
            }
        }

        private void RegisterDefinition(FactoryProductDefinition definition)
        {
            Debug.Assert(definition, this);

            if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
            {
                Debug.Log($"Registering product {definition.name}");
            }

            StartCoroutine(FillReserveRoutine(definition));
        }

        private void UnregisterDefinition(FactoryProductDefinition definition)
        {
            Debug.Assert(definition, this);

            if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
            {
                Debug.Log($"Unregistering product {definition.name}");
            }

            ClearReserve(definition);
        }

        // From a user perspective, not sure what this is doing....
        private void ResetManager()
        {
            if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
            {
                Debug.Log($"Reseting {this}");
            }

            ResetPoolObjects();

            FillInitialReserves();
        }

        private void ResetPoolObjects()
        {
            foreach (var key in m_productPerID.Keys)
            {
                foreach (var val in m_productPerID[key])
                {
                    val.ResetProduct();
                }
            }
        }

        private FactoryProduct Get_Internal(FactoryProductDefinition id)
        {
            FactoryProduct product = GetProductSync(id);
            if (product == null)
            {
                Debug.LogError($"Failed to get product for {id}.");
                return null;
            }

            product.IsProductActive = true;

            return product;
        }

        private IReadOnlyList<FactoryProduct> Get_Internal(FactoryProductDefinition definition, int count)
        {
            FactoryProduct[] products = new FactoryProduct[count];
            for (int i = 0; i < count; i++)
            {
                products[i] = Get_Internal(definition);
            }

            return products;
        }

        private ComponentT Get_Internal<ComponentT>(FactoryProductDefinition id)
            where ComponentT : Component
        {
            FactoryProduct product = GetProductSync(id);
            if (product == null)
            {
                Debug.LogError($"Failed to get product {id}.");
                return null;
            }

            ComponentT componentT = product.GetComponent<ComponentT>();
            if (componentT == null)
            {
                Debug.LogError($"Failed to get component {typeof(ComponentT).Name} on product {id}");
                return null;
            }

            product.IsProductActive = true;

            return componentT;
        }

        private IReadOnlyList<ComponentT> Get_Internal<ComponentT>(FactoryProductDefinition definition, int count)
            where ComponentT : Component
        {
            ComponentT[] products = new ComponentT[count];
            for (int i = 0; i < count; i++)
            {
                products[i] = Get_Internal<ComponentT>(definition);
            }

            return products;
        }

        private void GetAsync_Internal(FactoryProductDefinition id, System.Action<FactoryProduct> onCompleted)
        {
            StartCoroutine(GetAsyncRoutine(id, onCompleted));
        }

        private void GetAsync_Internal(FactoryProductDefinition id, int count, System.Action<IReadOnlyList<FactoryProduct>> onCompleted)
        {
            StartCoroutine(GetAsyncRoutine(id, count, onCompleted));
        }

        private void GetAsync_Internal<ComponentT>(FactoryProductDefinition id, System.Action<ComponentT> onCompleted)
            where ComponentT : Component
        {
            StartCoroutine(GetAsyncRoutine(id, onCompleted));
        }

        private void GetAsync_Internal<ComponentT>(FactoryProductDefinition id, int count, System.Action<IReadOnlyList<ComponentT>> onCompleted)
            where ComponentT : Component
        {
            StartCoroutine(GetAsyncRoutine(id, count, onCompleted));
        }

        private IEnumerator GetAsyncRoutine(FactoryProductDefinition id, System.Action<FactoryProduct> onCompleted)
        {
            FactoryProduct product = null;
            yield return GetProductRoutine(id, product);
            if (!product)
            {
                Debug.LogError($"Failed to get product {id}.");
                yield break;
            }

            product.IsProductActive = true;
            onCompleted.Invoke(product);
        }

        private IEnumerator GetAsyncRoutine(FactoryProductDefinition id, int count, System.Action<IReadOnlyList<FactoryProduct>> onCompleted)
        {
            FactoryProduct[] products = new FactoryProduct[count];
            for (int i = 0; i < count; i++)
            {
                yield return GetProductRoutine(id, products[i]);
                if (products[i] == null)
                {
                    // This should never happen, but better log anyway.
                    Debug.LogError($"Failed to get product[{i}] of {id}.");
                    yield break;
                }
                products[i].IsProductActive = true;
            }

            onCompleted?.Invoke(products);
        }

        private IEnumerator GetAsyncRoutine<ComponentT>(FactoryProductDefinition id, System.Action<ComponentT> onCompleted)
            where ComponentT : Component
        {
            FactoryProduct product = null;
            yield return GetProductRoutine(id, product);
            if (product == null)
            {
                Debug.LogError($"Failed to get product {id}.");
                yield break;
            }

            ComponentT componentT = product.GetComponent<ComponentT>();
            if (componentT == null)
            {
                Debug.LogError($"Failed to get component {typeof(ComponentT).Name} on product {id}");
                yield break;
            }
            product.IsProductActive = true;
            onCompleted.Invoke(componentT);
        }

        private IEnumerator GetAsyncRoutine<ComponentT>(FactoryProductDefinition id, int count, System.Action<IReadOnlyList<ComponentT>> onCompleted)
            where ComponentT : Component
        {
            ComponentT[] components = new ComponentT[count];
            FactoryProduct product = null;
            for (int i = 0; i < count; i++)
            {
                yield return GetProductRoutine(id, product);
                if (product == null)
                {
                    Debug.LogError($"Failed to get product[{i}] {id}.");
                    yield break;
                }

                components[i] = product.GetComponent<ComponentT>();
                if (components[i] == null)
                {
                    Debug.LogError($"Failed to get component {typeof(ComponentT).Name} on product[{i}] {id}");
                    yield break;
                }
                product.IsProductActive = true;
            }

            onCompleted?.Invoke(components);
        }

        private FactoryProduct GetProductSync(FactoryProductDefinition id)
        {
            if (!m_productPerID.ContainsKey(id))
            {
                // If this object don't exist yet in the pool, check if can lazy instantiate.
                if ((m_behaviour & PoolBehaviour.WarmsLazyInstancing) != 0)
                {
                    Debug.LogWarning($"Factory: Lazy instantiate of object '{id}'.", this);
                }

                FillReserve(id);
            }

            // Could be optimized for speed
            // In the future, might be nice to provide a search method injection per object
            // This way, high frequency life cycle object can spend a bit more memory to get
            // a faster search (log(n) instead of n, using a second map to monitor inactive objects).
            FactoryProduct product = m_productPerID[id].Find((obj) => { return !obj.IsProductActive; });

            if (product == null)
            {
                if ((m_behaviour & PoolBehaviour.AllowsLazyInstancing) == 0)
                {
                    Debug.LogWarning($"Factory: No more product '{id}' available! Toggle AllowsLazyInstancing if needed.", this);
                    return null;
                }

                if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
                {
                    Debug.Log($"Factory: Instantiating new batch of '{id}'", this);
                }

                FactoryProduct[] newProducts = InstantiateBatch(id, m_productPerID[id].Count + id.ReserveGrowCount);
                m_productPerID[id].AddRange(newProducts);
                product = newProducts[0];
            }

            return product;
        }

        private IEnumerator GetProductRoutine(FactoryProductDefinition id, FactoryProduct product)
        {
            if (!m_productPerID.ContainsKey(id))
            {
                yield return FillReserveRoutine(id);

                if ((m_behaviour & PoolBehaviour.WarmsLazyInstancing) != 0)
                {
                    Debug.LogWarning($"Factory: Lazy instantiate of object '{id}'.", this);
                }
            }

            product = m_productPerID[id].Find((obj) => { return !obj.IsProductActive; });

            if (product == null)
            {
                if ((m_behaviour & PoolBehaviour.AllowsLazyInstancing) == 0)
                {
                    Debug.LogWarning($"Cannot force instantiate, object of id: {id}. Skipped...");
                    yield break;
                }

                var handle = InstantiateBatchTask(id, m_productPerID[id].Count + id.ReserveGrowCount);
                yield return new WaitUntil(() => handle.GetAwaiter().IsCompleted);

                var result = handle.GetAwaiter().GetResult();
                m_productPerID[id].AddRange(result);
                product = result[0];
            }
        }

        private void Release_Internal(FactoryProduct instance)
        {
            instance.Release();
        }

        private FactoryProduct[] InstantiateBatch(FactoryProductDefinition definition, int count)
        {
            if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
            {
                Debug.Log($"Async Instantiating of {count} new {definition}.");
            }

            FactoryProduct[] out_array = new FactoryProduct[count];

            for (int i = 0; i < count; ++i)
            {
                out_array[i] = Instantiate(definition.Product, m_SpawnOffset, Quaternion.identity, m_reserveParent).GetComponent<FactoryProduct>();
                out_array[i].ResetProduct();
            }

            return out_array;
        }

        private async Awaitable<FactoryProduct[]> InstantiateBatchTask(FactoryProductDefinition definition, int count)
        {
            if ((m_behaviour & PoolBehaviour.LogDebug) != 0)
            {
                Debug.Log($"Async Instantiating of {count} new {definition}.");
            }

            var handle = InstantiateAsync(definition.Product, count, m_reserveParent, m_SpawnOffset, Quaternion.identity);
            await handle;

            for (int i = 0, c = handle.Result.Length; i < c; ++i)
            {
                handle.Result[i].ResetProduct();
            }

            return handle.Result;
        }

        // Generate the pool of object of the initial definitions.
        private void FillInitialReserves()
        {
            foreach (var collection in m_initialCollections)
            {
                FillReservesInBackbground(collection.Definitions);
            }

            FillReservesInBackbground(m_initialDefinitions);
        }

        private void FillReservesInBackbground(IReadOnlyList<FactoryProductDefinition> definitions)
        {
            foreach (var def in definitions)
            {
                StartCoroutine(FillReserveRoutine(def));
            }
        }

        private void FillReserve(FactoryProductDefinition def)
        {
            FactoryProductDefinition productId = def;
            if (!m_productPerID.ContainsKey(productId))
            {
                m_productPerID.Add(productId, new List<FactoryProduct>(def.ReserveSize));
            }

            // it's ok if we have more object.
            int reserveCountTarget = def.ReserveSize - m_productPerID[productId].Count;
            if (reserveCountTarget > 0)
            {
                FactoryProduct[] newProducts = InstantiateBatch(productId, reserveCountTarget);
                m_productPerID[productId].AddRange(newProducts);
            }
        }

        // Needs to be sync to avoid data race on the dictionary
        private IEnumerator FillReserveRoutine(FactoryProductDefinition def)
        {
            FactoryProductDefinition productId = def;
            if (!m_productPerID.ContainsKey(productId))
            {
                m_productPerID.Add(productId, new List<FactoryProduct>(def.ReserveSize));
            }

            // it's ok if we have more object.
            int reserveCountTarget = def.ReserveSize - m_productPerID[productId].Count;
            if (reserveCountTarget > 0)
            {
                var handle = InstantiateBatchTask(productId, reserveCountTarget);
                yield return new WaitUntil(() => handle.GetAwaiter().IsCompleted);

                m_productPerID[productId].AddRange(handle.GetAwaiter().GetResult());
            }
        }

        private void ClearReserve<TDefinition>(TDefinition definition) where TDefinition : FactoryProductDefinition
        {
            if (m_productPerID.ContainsKey(definition))
            {
                var list = m_productPerID[definition];
                foreach (var obj in list)
                {
                    Destroy(obj.gameObject);
                }
                m_productPerID[definition].Clear();
                m_productPerID.Remove(definition);
            }
        }

        private void Start()
        {
            if ((m_behaviour & PoolBehaviour.FillsPoolOnStart) != 0)
            {
                ResetManager();
            }
        }
    }
}
