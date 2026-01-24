using NobunAtelier.Gameplay;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace NobunAtelier
{
    /// <summary>
    /// Represents the registration state of the factory's resources.
    /// </summary>
    public enum ResourceState
    {
        /// <summary>
        /// Resources are not registered and not available for use.
        /// </summary>
        Unregistered,
        
        /// <summary>
        /// Resources are registered and ready for use.
        /// </summary>
        Registered
    }

    /// <summary>
    /// Abstract factory base class for managing ability loadable resources.
    /// Handles resource pooling, instantiation, and delayed release of components.
    /// </summary>
    /// <typeparam name="T">The component type to be instantiated.</typeparam>
    /// <typeparam name="LoadableT">The loadable component type that wraps the component.</typeparam>
    /// <typeparam name="ResourceT">The ability loadable resource type.</typeparam>
    /// <example>
    /// <code>
    /// // Using the VFX factory with lazy resource registration
    /// var factory = new AbilityLoadableVFXFactory();
    /// factory.ConfigureAndRegister(YourListOfResources);
    /// factory.Play(resource, transform);
    /// 
    /// // Using the VFX factory with eager resource registration
    /// var factory = new AbilityLoadableVFXFactory(YourListOfResources);
    /// factory.RegisterResources();
    /// factory.Play(resource, transform);
    /// factory.UnregisterResources();
    /// </code>
    /// </example>
    public abstract class AbilityLoadableResourceFactory<T, LoadableT, ResourceT> : IDisposable
        where T : Component
        where LoadableT : LoadableComponent<T>
        where ResourceT : AbilityLoadableResource<T, LoadableT>
    {
        /// <summary>
        /// Set to true to auto-release resources after playing using their <see cref="AbilityLoadableResource.ReleaseDelay"/>.
        /// For manual release, call <see cref="UnregisterResources"/>.
        /// </summary>
        public bool AsyncReleaseOnPlay { get; set; } = true;
        
        /// <summary>
        /// Number of products to warmup the factory with when registering resources.
        /// </summary>
        public int WarmupCount { get; set; } = 3;
        
        /// <summary>
        /// Gets the read-only list of target resources managed by this factory.
        /// </summary>
        public IReadOnlyList<ResourceT> TargetResources { get; private set; }
        
        private HashSet<string> m_RegisteredResources;
        private CancellationTokenSource m_CancellationTokenSource;
        
        /// <summary>
        /// Gets the cancellation token source for the factory.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource => m_CancellationTokenSource;

        private ResourceState m_State = ResourceState.Unregistered;
        public ResourceState ResourceState => m_State;

#if UNITY_EDITOR
        /// <summary>
        /// [Editor only] Gets the current debug information about resource usage.
        /// </summary>
        public ResourceDebugInfo DebugInfo { get; }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableResourceFactory{T, LoadableT, ResourceT}"/> class.
        /// </summary>
        public AbilityLoadableResourceFactory()
        {
            m_RegisteredResources = new HashSet<string>();
            m_State = ResourceState.Unregistered;
#if UNITY_EDITOR
            DebugInfo = new ResourceDebugInfo(this);
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableResourceFactory{T, LoadableT, ResourceT}"/> class.
        /// </summary>
        /// <param name="data">The list of resources to manage.</param>
        public AbilityLoadableResourceFactory(IReadOnlyList<ResourceT> data)
        {
            m_RegisteredResources = new HashSet<string>(data.Count);
            TargetResources = data;
            m_CancellationTokenSource = new CancellationTokenSource();
            m_State = ResourceState.Unregistered;
#if UNITY_EDITOR
            DebugInfo = new ResourceDebugInfo(this);
            DebugInfo.CancellationTokenCancelled = false;
#endif
        }

        /// <summary>
        /// Disposes of the factory by canceling the cancellation token source and unregistering the resources.
        /// </summary>
        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
            UnregisterResources();
            TargetResources = null;
            m_RegisteredResources = null;
        }

        /// <summary>
        /// Configure the factory with a new list of resources, unregisters the old resources and registers the new ones.
        /// </summary>
        /// <param name="data">The list of resources to configure the factory with.</param>
        public void ConfigureAndRegister(IReadOnlyList<ResourceT> data)
        {
            UnregisterResources();
            TargetResources = data;
            RegisterResources();
        }

        /// <summary>
        /// Registers all resources by adding them to the registered set.
        /// Optionally pre-warms the pool by instantiating and immediately releasing products.
        /// Resources that are already registered will be skipped.
        /// </summary>
        public void RegisterResources()
        {
            if (TargetResources == null || TargetResources.Count == 0)
            {
                return;
            }

            foreach (ResourceT resource in TargetResources)
            {
                if (m_RegisteredResources.Contains(resource.Resource.AssetGUID))
                {
                    continue;
                }

                m_RegisteredResources.Add(resource.Resource.AssetGUID);
                Warm(resource);
            }

            m_CancellationTokenSource = new CancellationTokenSource();
            m_State = ResourceState.Registered;
#if UNITY_EDITOR
            DebugInfo.RegisteredCount = m_RegisteredResources.Count;
            DebugInfo.CancellationTokenCancelled = false;
#endif
        }

        /// <summary>
        /// Unregisters all resources by clearing the registered resources set.
        /// Cancels any ongoing async operations.
        /// </summary>
        public void UnregisterResources()
        {
            if (m_RegisteredResources == null || m_RegisteredResources.Count == 0)
            {
                return;
            }

            // Cancel the cancellation token source to stop the release operations
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();

            m_RegisteredResources.Clear();
            m_State = ResourceState.Unregistered;
#if UNITY_EDITOR
            DebugInfo.RegisteredCount = 0;
            DebugInfo.PlayingCount = 0;
            DebugInfo.ReleasingCount = 0;
            DebugInfo.CancellationTokenCancelled = true;
#endif
        }

        /// <summary>
        /// Gets a product component from the pool for the specified resource.
        /// </summary>
        /// <param name="resource">The resource to get the product for.</param>
        /// <returns>The instantiated component product.</returns>
        public T GetProduct(ResourceT resource)
        {
            T product = GetNewProduct(resource.Resource);

            if (product == null)
            {
                Debug.LogError($"Can't get new product {resource.Resource}???");
            }

            return product;
        }

        /// <summary>
        /// Plays a specific resource on the specified target transform. If the resource has a start delay, it will be played asynchronously.
        /// </summary>
        /// <param name="resource">The resource to play.</param>
        /// <param name="target">The target transform to play the resource on.</param>
        public void Play(ResourceT resource, Transform target)
        {
            if (resource == null)
            {
                Debug.LogError($"Trying to play a null resource of type {typeof(T).Name}. Make sure to pass a valid resource.");
                return;
            }

            if (m_State == ResourceState.Unregistered)
            {
                RegisterResources();
                Debug.LogWarning($"Playing unregistered resources of type {typeof(T).Name}, lazy registration will be performed. " 
                    + "Fix this by registering the resources before playing them. This is slow and should be avoided.");
            }

            if (!m_RegisteredResources.Contains(resource.Resource.AssetGUID))
            {
                Debug.LogError($"Trying to play an unknown resource of type {typeof(T).Name}. Make sure to ConfigureAndRegister() resources.");
                return;
            }

            if (resource.StartDelay > 0)
            {
                PlayWithDelayAsync(resource, target, CancellationTokenSource.Token).FireAndForget();
            }
            else
            {
                Play_Internal(resource, target);
            }
        }

        /// <summary>
        /// Plays all resources on the specified target transform.
        /// </summary>
        /// <param name="target">The target transform to play the resources on.</param>
        public void PlayAll(Transform target)
        {
            for (int i = 0; i < TargetResources.Count; i++)
            {
                Play(TargetResources[i], target);
            }
        }

        private void Play_Internal(ResourceT resource, Transform target)
        {
            T product = GetProduct(resource);

            SetupBeforePlay(resource, product, target);

            PlayProduct(resource, product);

#if UNITY_EDITOR
            DebugInfo.PlayingCount++;
            DebugInfo.TotalPlayCount++;
#endif

            if (AsyncReleaseOnPlay)
            {
                ReleaseProductAsync(resource, product, CancellationTokenSource.Token).FireAndForget();
            }
        }

        /// <summary>
        /// Asynchronously plays a resource on the target transform after a delay.
        /// </summary>
        /// <param name="resource">The resource to play.</param>
        /// <param name="target">The target transform to play the resource on.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the delayed play operation.</param>
        /// <returns>An awaitable task.</returns>
        private async Awaitable PlayWithDelayAsync(ResourceT resource, Transform target, CancellationToken cancellationToken = default)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(resource.StartDelay, cancellationToken);

                if (resource != null)
                {
                    Play_Internal(resource, target);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected if the factory is destroyed or resource are manually unregistered.
                // Do nothing as the release will be handled by the factory's destructor.
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Releases the product component back to the pool.
        /// </summary>
        /// <param name="resource">The resource associated with the product.</param>
        /// <param name="product">The product component to release.</param>
        protected void ReleaseResource(ResourceT resource, T product)
        {
            ReleaseProduct(resource.Resource, product);
        }

        /// <summary>
        /// Asynchronously waits for the release delay before releasing the product.
        /// </summary>
        protected async virtual Awaitable ReleaseProductAsync(ResourceT resource, T product, CancellationToken cancellationToken = default)
        {
            if (product == null)
            {
                return;
            }

#if UNITY_EDITOR
            DebugInfo.ReleasingCount++;
#endif

            if (resource.ReleaseDelay <= 0)
            {
                ReleaseResource(resource, product);
#if UNITY_EDITOR
                DebugInfo.PlayingCount--;
                DebugInfo.ReleasingCount--;
                DebugInfo.TotalReleaseCount++;
#endif
                return;
            }
            
            try
            {
                await Awaitable.WaitForSecondsAsync(resource.ReleaseDelay, cancellationToken);

                // Check again as product might have been released by the time we got here
                if (product != null)
                {
                    ReleaseResource(resource, product);
                }

#if UNITY_EDITOR
                DebugInfo.PlayingCount--;
                DebugInfo.ReleasingCount--;
                DebugInfo.TotalReleaseCount++;
#endif
            }
            catch (OperationCanceledException)
            {
                // Expected if the factory is destroyed or resource are manually unregistered.
                // Do nothing as the release will be handled by the factory's destructor.
#if UNITY_EDITOR
                DebugInfo.ReleasingCount--;
#endif
            }
            catch (ObjectDisposedException)
            {
                // Expected if the factory is destroyed
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
#if UNITY_EDITOR
                DebugInfo.ReleasingCount--;
#endif
            }
        }

        /// <summary>
        /// Warmup the factory with the specified resource.
        /// </summary>
        /// <param name="resource">The resource to warmup the factory with.</param>
        protected abstract void Warm(ResourceT resource);

        /// <summary>
        /// Setup the product before playing it. Can be overridden to add custom setup logic.
        /// </summary>
        /// <param name="resource">The resource to play.</param>
        /// <param name="product">The product to setup.</param>
        /// <param name="target">The target transform to play the resource on.</param>
        protected virtual void SetupBeforePlay(ResourceT resource, T product, Transform target) 
        {
            if (target == null)
            {
                return;
            }

            product.transform.position = target.position;
        }

        /// <summary>
        /// Plays the instantiated product component. Implementation specific to the resource type.
        /// </summary>
        /// <param name="product">The product component to play.</param>
        protected abstract void PlayProduct(ResourceT resource, T product); 

        /// <summary>
        /// Creates a new product component from the loadable resource.
        /// </summary>
        /// <param name="resource">The loadable resource to instantiate.</param>
        /// <returns>The newly created component product.</returns>
        protected abstract T GetNewProduct(LoadableT resource);

        /// <summary>
        /// Releases the product component back to the pool.
        /// </summary>
        /// <param name="resource">The loadable resource associated with the product.</param>
        /// <param name="product">The product component to release.</param>
        protected abstract void ReleaseProduct(LoadableT resource, T product);

#if UNITY_EDITOR
        /// <summary>
        /// Debug information for tracking resource usage in the editor.
        /// </summary>
        public class ResourceDebugInfo
        {
            private AbilityLoadableResourceFactory<T, LoadableT, ResourceT> m_Factory;
            public ResourceDebugInfo(AbilityLoadableResourceFactory<T, LoadableT, ResourceT> factory)
            {
                m_Factory = factory;
            }

            /// <summary>
            /// Number of registered resource products in the factory.
            /// </summary>
            public int RegisteredCount;
            
            /// <summary>
            /// Number of resources currently being played (active).
            /// </summary>
            public int PlayingCount;
            
            /// <summary>
            /// Number of resources currently being released (delayed cleanup).
            /// </summary>
            public int ReleasingCount;
            
            /// <summary>
            /// Total number of play operations since factory creation.
            /// </summary>
            public int TotalPlayCount;
            
            /// <summary>
            /// Total number of release operations since factory creation.
            /// </summary>
            public int TotalReleaseCount;

            public bool CancellationTokenCancelled;

            public override string ToString()
            {
                string registeredResourcesString = "";
                var registeredResources = m_Factory?.TargetResources?.Select(r => r.Resource?.editorAsset?.name);
                if (registeredResources != null)
                {
                    registeredResourcesString = $"\nRegistered Resources: \n- {string.Join("\n- ", registeredResources)}";
                }

                return $"({m_Factory?.GetType().Name}) - State: {m_Factory?.ResourceState} - CancellationTokenCancelled: {CancellationTokenCancelled}" + 
                    $"\nRegistered: {RegisteredCount}, Playing: {PlayingCount}, Releasing: {ReleasingCount}" +	
                    $"\nTotalPlayed: {TotalPlayCount}, TotalReleased: {TotalReleaseCount}" +
                    registeredResourcesString;
            }
        }
#endif
    }

    /// <summary>
    /// Abstract factory for managing ability loadable transform resources with spatial placement.
    /// Handles transform operations including position, rotation, and scale relative to a target.
    /// </summary>
    /// <typeparam name="T">The component type to be instantiated.</typeparam>
    /// <typeparam name="LoadableT">The loadable component type that wraps the component.</typeparam>
    /// <typeparam name="ResourceT">The ability loadable transform resource type.</typeparam>
    public abstract class AbilityLoadableTransformFactory<T, LoadableT, ResourceT>
        : AbilityLoadableResourceFactory<T, LoadableT, ResourceT>
        where T : Component
        where LoadableT : LoadableComponent<T>
        where ResourceT : AbilityLoadableTransform<T, LoadableT>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableTransformFactory{T, LoadableT, ResourceT}"/> class.
        /// </summary>
        public AbilityLoadableTransformFactory()
            : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableTransformFactory{T, LoadableT, ResourceT}"/> class.
        /// </summary>
        /// <param name="data">The list of resources to manage.</param>
        public AbilityLoadableTransformFactory(IReadOnlyList<ResourceT> data)
            : base(data)
        { }

        /// <summary>
        /// Setup the product before playing it by positioning, rotating, and scaling it relative to the target transform.
        /// </summary>
        protected override void SetupBeforePlay(ResourceT resource, T product, Transform target)
        {
            if (target == null)
            {
                return;
            }

            product.transform.position = target.position + target.TransformDirection(resource.PositionOffset);
            product.transform.rotation = target.rotation * Quaternion.Euler(resource.RotationOffset);
            product.transform.localScale = resource.Scale;
        }
    }

    /// <summary>
    /// Factory for managing particle system visual effects (VFX) for abilities.
    /// Handles instantiation, pooling, and playback of particle systems.
    /// </summary>
    public class AbilityLoadableVFXFactory
        : AbilityLoadableTransformFactory<ParticleSystem, LoadableParticleSystem, AbilityLoadableParticleSystem>
    {
        /// <summary>
        /// If set above 0, will check every <see cref="AutoReleaseCheckDelay"/> seconds if the particle system has finished playing
        /// instead of using the <see cref="AbilityLoadableResource{T, LoadableT}.ReleaseDelay"/>.
        /// </summary>
        public float AutoReleaseCheckDelay { get; set; } = 0.5f;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableVFXFactory"/> class.
        /// </summary>
        public AbilityLoadableVFXFactory()
            : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableVFXFactory"/> class.
        /// </summary>
        /// <param name="data">The list of particle system resources to manage.</param>
        public AbilityLoadableVFXFactory(IReadOnlyList<AbilityLoadableParticleSystem> data)
            : base(data)
        { }

        protected override void Warm(AbilityLoadableParticleSystem resource)
        {
            LoadableParticleSystemPoolFactory.CreateFactory(resource.Resource, WarmupCount);
        }

        protected override ParticleSystem GetNewProduct(LoadableParticleSystem resource)
        {
            var product = LoadableParticleSystemPoolFactory.Get(resource);
            product.Stop();
            return product;
        }

        protected override void ReleaseProduct(LoadableParticleSystem resource, ParticleSystem product)
        {
            if (product == null)
            {
                return;
            }

            if (product.isPlaying)
            {
                product.Stop();
            }

            LoadableParticleSystemPoolFactory.Release(resource, product);
        }

        protected override void PlayProduct(AbilityLoadableParticleSystem resource, ParticleSystem product) => product.Play(true);

        protected override async Awaitable ReleaseProductAsync(AbilityLoadableParticleSystem resource, ParticleSystem product, CancellationToken cancellationToken = default)
        {
            if (product == null)
            {
                return;
            }

#if UNITY_EDITOR
            DebugInfo.ReleasingCount++;
#endif

            if (resource.ReleaseDelay <= 0)
            {
                ReleaseResource(resource, product);
#if UNITY_EDITOR
                DebugInfo.PlayingCount--;
                DebugInfo.ReleasingCount--;
                DebugInfo.TotalReleaseCount++;
#endif
                return;
            }

            try
            {
                if (AutoReleaseCheckDelay > 0)
                {
                    while (product.isPlaying)
                    {
                        await Awaitable.WaitForSecondsAsync(AutoReleaseCheckDelay, cancellationToken);
                        if (product == null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    await Awaitable.WaitForSecondsAsync(resource.ReleaseDelay, cancellationToken);
                }

                if (product != null)
                {
                    ReleaseResource(resource, product);
                }

#if UNITY_EDITOR
                DebugInfo.PlayingCount--;
                DebugInfo.ReleasingCount--;
                DebugInfo.TotalReleaseCount++;
#endif
            }
            catch (OperationCanceledException)
            {
                // Expected if the factory is destroyed or resource are manually unregistered.
                // Do nothing as the release will be handled by the factory's destructor.
#if UNITY_EDITOR
                DebugInfo.ReleasingCount--;
#endif
            }
            catch (ObjectDisposedException)
            {
                // Expected if the factory is destroyed
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
#if UNITY_EDITOR
                DebugInfo.ReleasingCount--;
#endif
            }
        }
    }

    /// <summary>
    /// Factory for managing hitbox components for abilities.
    /// Handles hitbox instantiation, pooling, configuration, and hit event management.
    /// </summary>
    public class AbilityLoadableHitboxFactory
        : AbilityLoadableTransformFactory<Hitbox, LoadableHitbox, AbilityLoadableHitbox>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableHitboxFactory"/> class.
        /// </summary>
        public AbilityLoadableHitboxFactory()
            : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableHitboxFactory"/> class.
        /// Pre-warms the pool by instantiating and immediately releasing all hitboxes during construction.
        /// </summary>
        /// <param name="data">The list of hitbox resources to manage.</param>
        public AbilityLoadableHitboxFactory(IReadOnlyList<AbilityLoadableHitbox> data)
            : base(data)
        {
            // Pre-warm the pool by getting and releasing products
            foreach (var item in TargetResources)
            {
                var hitbox = GetNewProduct(item.Resource);
                ReleaseProduct(item.Resource, hitbox);
            }
        }

        protected override void Warm(AbilityLoadableHitbox resource)
        {
            LoadableHitboxPoolFactory.CreateFactory(resource.Resource, WarmupCount);
        }

        protected override Hitbox GetNewProduct(LoadableHitbox resource)
        {
            return LoadableHitboxPoolFactory.Get(resource);
        }

        protected override void ReleaseProduct(LoadableHitbox resource, Hitbox product)
        {
            if (product == null)
            {
                return;
            }

            LoadableHitboxPoolFactory.Release(resource, product);
        }

        /// <summary>
        /// Activates the hitbox by calling its HitBegin method.
        /// </summary>
        /// <param name="product">The hitbox to activate.</param>
        protected override void PlayProduct(AbilityLoadableHitbox resource, Hitbox product) => product.HitBegin();

        /// <summary>
        /// Adds a listener to all hitboxes' OnHit events.
        /// </summary>
        /// <param name="listener">The callback to invoke when any hitbox hits something.</param>
        public void AddListenerOnHit(UnityEngine.Events.UnityAction<HitInfo> listener)
        {
            foreach (var resources in TargetResources)
            {
                var hitbox = GetProduct(resources);
                hitbox.OnHit.AddListener(listener);
            }
        }

        /// <summary>
        /// Configures all hitboxes with hit definition, target definition, owner, and initial transform.
        /// </summary>
        /// <param name="origin">The origin transform for positioning and rotation.</param>
        /// <param name="target">The target definition specifying what can be hit.</param>
        /// <param name="teamModule">The team module that owns these hitboxes.</param>
        /// <param name="hitDefinition">The hit definition containing damage and hit behavior data.</param>
        public void SetupHitboxes(Transform origin, TeamDefinition.Target target, TeamModule teamModule, HitDefinition hitDefinition)
        {
            foreach (var item in TargetResources)
            {
                Hitbox hitbox = GetProduct(item);
                hitbox.SetHitDefinition(hitDefinition);
                hitbox.SetTargetDefinition(target);
                hitbox.SetOwner(teamModule);

                hitbox.transform.localPosition = origin.position + origin.TransformDirection(item.PositionOffset);
                hitbox.transform.localRotation = origin.rotation * Quaternion.Euler(item.RotationOffset);
                hitbox.transform.localScale = item.Scale;
            }
        }

        /// <summary>
        /// Updates the position and rotation of all hitboxes relative to the origin transform.
        /// Useful for hitboxes that need to follow a moving character.
        /// </summary>
        /// <param name="origin">The origin transform for positioning and rotation.</param>
        public void UpdateHitbox(Transform origin)
        {
            foreach (var item in TargetResources)
            {
                Hitbox hitbox = GetProduct(item);
                hitbox.transform.localPosition = origin.position + origin.TransformDirection(item.PositionOffset);
                hitbox.transform.localRotation = origin.rotation * Quaternion.Euler(item.RotationOffset);
            }
        }
    }

    /// <summary>
    /// Factory for managing audio source components for ability sound effects (SFX).
    /// Handles instantiation, pooling, and playback of audio sources.
    /// </summary>
    public class AbilityLoadableSFXFactory
        : AbilityLoadableResourceFactory<AudioSource, LoadableAudioSource, AbilityLoadableAudioSource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableSFXFactory"/> class.
        /// </summary>
        public AbilityLoadableSFXFactory()
            : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityLoadableSFXFactory"/> class.
        /// </summary>
        /// <param name="data">The list of audio source resources to manage.</param>
        public AbilityLoadableSFXFactory(IReadOnlyList<AbilityLoadableAudioSource> data)
            : base(data)
        { }

        protected override void Warm(AbilityLoadableAudioSource resource)
        {
            LoadableAudioSourcePoolFactory.CreateFactory(resource.Resource, WarmupCount);
        }

        protected override AudioSource GetNewProduct(LoadableAudioSource resource)
        {
            var product = LoadableAudioSourcePoolFactory.Get(resource);
            product.playOnAwake = false;
            return product;
        }

        protected override void ReleaseProduct(LoadableAudioSource resource, AudioSource product)
        {
            if (product == null)
            {
                return;
            }

            if (product.isPlaying)
            {
                product.Stop();
            }

            LoadableAudioSourcePoolFactory.Release(resource, product);
        }

        /// <summary>
        /// Setup the product before playing it by positioning the audio source and playing it.
        /// </summary>
        protected override void SetupBeforePlay(AbilityLoadableAudioSource resource, AudioSource product, Transform target)
        {
            if (target == null)
            {
                return;
            }

            product.transform.position = target.position + resource.AudioOffset;            
        }

        protected override void PlayProduct(AbilityLoadableAudioSource resource, AudioSource product) => product.Play();
    }
}