using UnityEngine;

namespace NobunAtelier
{
    [System.Serializable]
    public class LoadableAudioSource : LoadableGameObjectComponent<AudioSource>
    {
        public LoadableAudioSource(string guid) : base(guid)
        { }
    }

    public class LoadableAudioSourcePoolFactory :
        LoadableComponentPoolFactory<AudioSource, LoadableAudioSource, LoadableAudioSourcePoolFactory>
    { }
}