using NobunAtelier.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [System.Serializable]
    public class AbilityLoadableResource<T, LoadableT>
    where T : Component
    where LoadableT : LoadableComponent<T>
    {
        [SerializeField] private LoadableT m_resource;
        [Tooltip("How much time should the instantiated object be kept alive after the ability effect ended.")]
        [SerializeField] private float m_releaseDelay = 1f;
        public LoadableT Resource => m_resource;
        public float ReleaseDelay => m_releaseDelay;
    }

    public abstract class AbilityLoadableResourceFactory<T, LoadableT, ResourceT>
        where T : Component
        where LoadableT : LoadableComponent<T>
        where ResourceT : AbilityLoadableResource<T, LoadableT>
    {
        public IReadOnlyList<ResourceT> TargetResources { get; private set; }
        private Dictionary<ResourceT, T> m_map;

        public AbilityLoadableResourceFactory(IReadOnlyList<ResourceT> data)
        {
            m_map = new Dictionary<ResourceT, T>(data.Count);
            TargetResources = data;
        }

        ~AbilityLoadableResourceFactory()
        {
            UnregisterResources();
            TargetResources = null;
            m_map = null;
        }

        public void RegisterResources()
        {
            if (TargetResources == null || TargetResources.Count == 0)
            {
                return;
            }

            foreach (ResourceT r in TargetResources)
            {
                if (m_map.TryGetValue(r, out var product))
                {
                    continue;
                }

                m_map.Add(r, GetNewProduct(r.Resource));
            }
        }

        public void UnregisterResources()
        {
            if (m_map == null || m_map.Count == 0)
            {
                return;
            }

            foreach (ResourceT r in TargetResources)
            {
                if (m_map.TryGetValue(r, out var product))
                {
                    CoroutineManager.Start(ReleaseResourceDelayedRountine(r, product));
                }
            }
            m_map.Clear();
        }

        public T GetProduct(ResourceT resource)
        {
            if (m_map != null && m_map.TryGetValue(resource, out var products))
            {
                return products;
            }

            products = GetNewProduct(resource.Resource);
            m_map.Add(resource, products);

            if (products == null)
            {
                Debug.LogError($"Can't get new product {resource.Resource}???");
            }

            return products;
        }

        public abstract void PlayAll(Transform target);

        public abstract void Play(ResourceT resource, Transform target);

        public abstract IEnumerator PlayDelayedRoutine(ResourceT resource, Transform target);

        protected abstract T GetNewProduct(LoadableT resource);

        protected abstract void ReleaseProduct(LoadableT resource, T product);

        private IEnumerator ReleaseResourceDelayedRountine(ResourceT resource, T product)
        {
            if (resource.ReleaseDelay <= 0)
            {
                ReleaseProduct(resource.Resource, product);
                yield break;
            }

            yield return new WaitForSeconds(resource.ReleaseDelay);
            ReleaseProduct(resource.Resource, product);
        }
    }


    [System.Serializable]
    public class AbilityLoadableTransform<T, LoadableT> : AbilityLoadableResource<T, LoadableT>
        where T : Component
        where LoadableT : LoadableComponent<T>
    {
        [SerializeField] private float m_startDelay;

        [SerializeField, Tooltip("Offset relative to the actor spawning the particle.")]
        private Vector3 m_positionOffset = Vector3.zero;

        [SerializeField] private Vector3 m_rotationOffset = Vector3.zero;
        [SerializeField] private Vector3 m_scale = Vector3.one;

        public float StartDelay => m_startDelay;
        public Vector3 PositionOffset => m_positionOffset;
        public Vector3 RotationOffset => m_rotationOffset;
        public Vector3 Scale => m_scale;
    }

    [System.Serializable]
    public class AbilityLoadableParticleSystem : AbilityLoadableTransform<ParticleSystem, LoadableParticleSystem>
    { }

    [System.Serializable]
    public class AbilityLoadableHitbox : AbilityLoadableTransform<Hitbox, LoadableHitbox>
    { }

    [System.Serializable]
    public class AbilityLoadableAudioSource : AbilityLoadableResource<AudioSource, LoadableAudioSource>
    {
        [SerializeField] private float m_startDelay;

        [SerializeField, Tooltip("Offset relative to the actor spawning the audio.")]
        private Vector3 m_positionOffset = Vector3.zero;

        public float StartDelay => m_startDelay;
        public Vector3 AudioOffset => m_positionOffset;
    }


    public abstract class AbilityLoadableTransformFactory<T, LoadableT, ResourceT>
        : AbilityLoadableResourceFactory<T, LoadableT, ResourceT>
        where T : Component
        where LoadableT : LoadableComponent<T>
        where ResourceT : AbilityLoadableTransform<T, LoadableT>
    {
        public AbilityLoadableTransformFactory(IReadOnlyList<ResourceT> data)
            : base(data)
        { }

        public override void Play(ResourceT resource, Transform target)
        {
            T product = GetProduct(resource);
            product.transform.position = target.position + target.TransformDirection(resource.PositionOffset);
            product.transform.rotation = target.rotation * Quaternion.Euler(resource.RotationOffset);
            product.transform.localScale = resource.Scale;
            PlayProduct(product);
        }

        public override void PlayAll(Transform target)
        {
            for (int i = 0; i < TargetResources.Count; i++)
            {
                ResourceT data = TargetResources[i];
                if (data.StartDelay > 0)
                {
                    CoroutineManager.Start(PlayDelayedRoutine(data, target));
                }
                else
                {
                    Play(data, target);
                }
            }
        }

        public override IEnumerator PlayDelayedRoutine(ResourceT resource, Transform target)
        {
            yield return new WaitForSeconds(resource.StartDelay);
            Play(resource, target);
        }

        protected abstract void PlayProduct(T product);
    }

    public class AbilityLoadableVFXFactory
        : AbilityLoadableTransformFactory<ParticleSystem, LoadableParticleSystem, AbilityLoadableParticleSystem>
    {
        public AbilityLoadableVFXFactory(IReadOnlyList<AbilityLoadableParticleSystem> data)
            : base(data)
        { }

        protected override ParticleSystem GetNewProduct(LoadableParticleSystem resource)
        {
            var product = LoadableParticleSystemPoolFactory.Get(resource);
            product.Stop();
            return product;
        }

        protected override void ReleaseProduct(LoadableParticleSystem resource, ParticleSystem product)
        {
            if (product.isPlaying)
            {
                product.Stop();
            }

            LoadableParticleSystemPoolFactory.Release(resource, product);
        }

        protected override void PlayProduct(ParticleSystem product)
        {
            product.Play(true);
        }
    }

    public class AbilityLoadableHitboxFactory
        : AbilityLoadableTransformFactory<Hitbox, LoadableHitbox, AbilityLoadableHitbox>
    {
        public AbilityLoadableHitboxFactory(IReadOnlyList<AbilityLoadableHitbox> data)
            : base(data)
        {
            foreach (var item in TargetResources)
            {
                var hitbox = GetProduct(item);
            }
        }

        protected override Hitbox GetNewProduct(LoadableHitbox resource)
        {
            return LoadableHitboxPoolFactory.Get(resource);
        }

        protected override void ReleaseProduct(LoadableHitbox resource, Hitbox product)
        {
            LoadableHitboxPoolFactory.Release(resource, product);
        }

        protected override void PlayProduct(Hitbox product)
        {
            product.HitBegin();
        }

        public void AddListenerOnHit(UnityEngine.Events.UnityAction<HitInfo> listener)
        {
            foreach (var resources in TargetResources)
            {
                var hitbox = GetProduct(resources);
                hitbox.OnHit.AddListener(listener);
            }
        }

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

    public class AbilityLoadableSFXFactory
        : AbilityLoadableResourceFactory<AudioSource, LoadableAudioSource, AbilityLoadableAudioSource>
    {
        public AbilityLoadableSFXFactory(IReadOnlyList<AbilityLoadableAudioSource> data)
            : base(data)
        { }

        protected override AudioSource GetNewProduct(LoadableAudioSource resource)
        {
            var product = LoadableAudioSourcePoolFactory.Get(resource);
            product.playOnAwake = false;
            return product;
        }

        protected override void ReleaseProduct(LoadableAudioSource resource, AudioSource product)
        {
            if (product.isPlaying)
            {
                product.Stop();
            }

            LoadableAudioSourcePoolFactory.Release(resource, product);
        }

        public override void Play(AbilityLoadableAudioSource resource, Transform target)
        {
            var product = GetProduct(resource);

            if (product == null)
            {
                return;
            }

            product.transform.position = target.position + resource.AudioOffset;
            product.Play();
        }


        public override void PlayAll(Transform target)
        {
            for (int i = 0; i < TargetResources.Count; i++)
            {
                AbilityLoadableAudioSource data = TargetResources[i];
                if (data.StartDelay > 0)
                {
                    CoroutineManager.Start(PlayDelayedRoutine(data, target));
                }
                else
                {
                    Play(data, target);
                }
            }
        }

        public override IEnumerator PlayDelayedRoutine(AbilityLoadableAudioSource resource, Transform target)
        {
            yield return new WaitForSeconds(resource.StartDelay);
            Play(resource, target);
        }
    }
}
