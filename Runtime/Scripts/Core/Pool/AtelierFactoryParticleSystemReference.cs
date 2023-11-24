using UnityEngine;

namespace NobunAtelier
{
    [System.Serializable]
    public class AssetReferenceParticleSystem : AssetReferenceGameObjectComponentT<ParticleSystem>
    {
        public AssetReferenceParticleSystem(string guid) : base(guid)
        { }
    }

    public class AtelierFactoryParticleSystemReference
        : AtelierFactoryGameObjectReferenceT<ParticleSystem, AssetReferenceParticleSystem, AtelierFactoryParticleSystemReference>
    { }
}