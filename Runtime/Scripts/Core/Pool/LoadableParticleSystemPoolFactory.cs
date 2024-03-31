using UnityEngine;

namespace NobunAtelier
{
    [System.Serializable]
    public class LoadableParticleSystem : LoadableComponent<ParticleSystem>
    {
        public LoadableParticleSystem(string guid) : base(guid)
        { }
    }

    public class LoadableParticleSystemPoolFactory
        : LoadableComponentPoolFactory<ParticleSystem, LoadableParticleSystem, LoadableParticleSystemPoolFactory>
    { }
}