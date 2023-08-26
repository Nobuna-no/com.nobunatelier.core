using UnityEngine;

namespace NobunAtelier
{
    [System.Serializable]
    public class AssetReferenceAudioSource : AssetReferenceGameObjectComponentT<AudioSource>
    {
        public AssetReferenceAudioSource(string guid) : base(guid)
        { }
    }

    public class AtelierFactoryAudioSourceReference :
        AtelierFactoryGameObjectReferenceT<AudioSource, AssetReferenceAudioSource, AtelierFactoryAudioSourceReference>
    { }
}