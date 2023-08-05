using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;

namespace NobunAtelier
{
    public class AudioResourceDefinition : DataDefinition 
    {
        public bool StartDelayed => m_canStartDelayedToPreventCPUOverhead;

        [SerializeField]
        private bool m_canStartDelayedToPreventCPUOverhead = true;
    }

    public class AudioDefinition : AudioResourceDefinition
    {
        public AssetReference AudioAssetReference => m_audioAssetReference;

        [SerializeField]
        private AssetReference m_audioAssetReference;

        public AudioMixerGroup MixerGroup => m_mixerGroup;

        [SerializeField]
        private AudioMixerGroup m_mixerGroup;
        
        public float Volume => m_volume;

        [SerializeField, Range(0f, 1f)]
        private float m_volume = 1.0f;

        public bool Loop => m_loop;

        [SerializeField]
        private bool m_loop = false;
    }
}