using NobunAtelier.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    /// <summary>
    /// Base class for ability resources that can be loaded and instantiated on demand.
    /// Manages a loadable component reference with a configurable release delay.
    /// </summary>
    /// <typeparam name="T">The component type to be instantiated.</typeparam>
    /// <typeparam name="LoadableT">The loadable component type that wraps the component.</typeparam>
    [System.Serializable]
    public class AbilityLoadableResource<T, LoadableT>
        where T : Component
        where LoadableT : LoadableComponent<T>
    {
        [FormerlySerializedAs("m_resource")]
        [SerializeField] private LoadableT m_Resource;
        [Tooltip("How much time should the instantiated object be kept alive after the ability effect ended.")]
        [FormerlySerializedAs("m_startDelay")]
        [SerializeField] private float m_StartDelay;
        [FormerlySerializedAs("m_releaseDelay")]
        [SerializeField] private float m_ReleaseDelay = 1f;

        /// <summary>
        /// Gets the loadable resource reference.
        /// </summary>
        public LoadableT Resource => m_Resource;
        
        /// <summary>
        /// Gets the delay in seconds before the resource is played.
        /// </summary>
        public float StartDelay => m_StartDelay;

        /// <summary>
        /// Gets the delay in seconds before the instantiated object is released back to the pool.
        /// </summary>
        public float ReleaseDelay => m_ReleaseDelay;
    }

    /// <summary>
    /// Extends <see cref="AbilityLoadableResource{T, LoadableT}"/> with transform data for positioning, rotation, and scaling.
    /// Used for resources that need spatial placement relative to the spawning actor.
    /// </summary>
    /// <typeparam name="T">The component type to be instantiated.</typeparam>
    /// <typeparam name="LoadableT">The loadable component type that wraps the component.</typeparam>
    [System.Serializable]
    public class AbilityLoadableTransform<T, LoadableT> : AbilityLoadableResource<T, LoadableT>
        where T : Component
        where LoadableT : LoadableComponent<T>
    {
        [FormerlySerializedAs("m_positionOffset")]
        [SerializeField, Tooltip("Offset relative to the actor spawning the particle.")]
        private Vector3 m_PositionOffset = Vector3.zero;

        [FormerlySerializedAs("m_rotationOffset")]
        [SerializeField] private Vector3 m_RotationOffset = Vector3.zero;
        [FormerlySerializedAs("m_scale")]
        [SerializeField] private Vector3 m_Scale = Vector3.one;

        
        /// <summary>
        /// Gets the position offset relative to the spawning actor.
        /// </summary>
        public Vector3 PositionOffset => m_PositionOffset;
        
        /// <summary>
        /// Gets the rotation offset in Euler angles.
        /// </summary>
        public Vector3 RotationOffset => m_RotationOffset;
        
        /// <summary>
        /// Gets the scale of the instantiated object.
        /// </summary>
        public Vector3 Scale => m_Scale;
    }

    /// <summary>
    /// Loadable resource for particle system components.
    /// </summary>
    [System.Serializable]
    public class AbilityLoadableParticleSystem : AbilityLoadableTransform<ParticleSystem, LoadableParticleSystem>
    { }

    /// <summary>
    /// Loadable resource for hitbox components.
    /// </summary>
    [System.Serializable]
    public class AbilityLoadableHitbox : AbilityLoadableTransform<Hitbox, LoadableHitbox>
    { }

    /// <summary>
    /// Loadable resource for audio source components.
    /// </summary>
    [System.Serializable]
    public class AbilityLoadableAudioSource : AbilityLoadableResource<AudioSource, LoadableAudioSource>
    {
        [FormerlySerializedAs("m_positionOffset")]
        [SerializeField, Tooltip("Offset relative to the actor spawning the audio.")]
        private Vector3 m_PositionOffset = Vector3.zero;

        /// <summary>
        /// Gets the position offset relative to the spawning actor.
        /// </summary>
        public Vector3 AudioOffset => m_PositionOffset;
    }
}
