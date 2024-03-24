using UnityEngine;

namespace NobunAtelier
{
    [System.Serializable]
    public class LodableParticleSystem : LoadableGameObjectComponent<ParticleSystem>
    {
        public LodableParticleSystem(string guid) : base(guid)
        { }
    }

    public class LoadableParticleSystemPoolFactory
        : LoadableComponentPoolFactory<ParticleSystem, LodableParticleSystem, LoadableParticleSystemPoolFactory>
    { }
}