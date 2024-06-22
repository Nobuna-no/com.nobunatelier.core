using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Allows children to sealed MonoBehaviour Initialization mechanisms.
    /// </summary>
    public abstract class VirtualFactoryProductMonoBehaviour : MonoBehaviour
    {
        protected virtual void OnProductReset()
        { }

        protected virtual void OnProductActivation()
        { }

        protected virtual void OnProductDeactivation()
        { }

        /// <summary>
        /// DO NOT USE IT. DataDrivenPoolFactory is handling the product lifecycle.
        /// Override OnProductActivation or OnProductReset instead.
        /// </summary>
        protected abstract void Awake();

        /// <summary>
        /// DO NOT USE IT. DataDrivenPoolFactory is handling the product lifecycle.
        /// Override OnProductActivation or OnProductReset instead.
        /// </summary>
        protected abstract void Start();

        /// <summary>
        /// DO NOT USE IT. DataDrivenPoolFactory is handling the product lifecycle.
        /// Override OnProductActivation instead.
        /// </summary>
        protected abstract void OnEnable();

        /// <summary>
        /// DO NOT USE IT. DataDrivenPoolFactory is handling the product lifecycle.
        /// Override OnProductDeactivation instead.
        /// </summary>
        protected abstract void OnDisable();
    }

    /// <summary>
    /// Provides basic life-cycle mechanism to an object to be used by a Factory.
    /// </summary>
    [DisallowMultipleComponent]
    public class FactoryProduct : VirtualFactoryProductMonoBehaviour
    {
        /// <summary>
        /// Called when the object is reset by the pool.
        /// </summary>
        public event Action onProductReset = null;

        /// <summary>
        /// Called each time the object is activated by the pool.
        /// </summary>
        public event Action onProductActivation = null;

        /// <summary>
        /// Called each time the object is deactivated by the pool.
        /// </summary>
        public event Action onProductDeactivation = null;

        /// <summary>
        /// Call Release or use this value to activate and deactivate the object.
        /// </summary>
        public bool IsProductActive
        {
            get
            {
                return gameObject.activeSelf;
            }
            set
            {
                if (IsProductActive == value)
                    return;

                if (value)
                {
                    onProductActivation?.Invoke();
                }
                else
                {
                    onProductDeactivation?.Invoke();
                }

                gameObject.SetActive(value);
            }
        }

        public Vector3 Position
        {
            get
            {
                return transform.position;
            }
            set
            {
                transform.position = value;
            }
        }

        public void Release()
        {
            IsProductActive = false;
        }

        public void ResetProduct()
        {
            onProductReset?.Invoke();
            // Manually disable the object to not call OnDeactivation.
            gameObject.SetActive(false);
        }

        protected override sealed void Awake()
        {
            onProductReset += OnProductReset;
            onProductActivation += OnProductActivation;
            onProductDeactivation += OnProductDeactivation;
        }

        protected override sealed void Start()
        { }

        protected override sealed void OnEnable()
        { }

        protected override sealed void OnDisable()
        { }
    }
}
