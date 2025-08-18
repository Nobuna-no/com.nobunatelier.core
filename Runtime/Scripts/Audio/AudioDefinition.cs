using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AudioResourceDefinition : DataDefinition
    {
        public bool CanStartDelayed => m_CanStartDelayedToPreventCPUOverhead;

        [SerializeField]
        [FormerlySerializedAs("m_canStartDelayedToPreventCPUOverhead")]
        private bool m_CanStartDelayedToPreventCPUOverhead = true;
    }

    public class AudioDefinition : AudioResourceDefinition
    {
        public AssetReference AudioAssetReference => m_AudioAssetReference;

        [SerializeField]
        [FormerlySerializedAs("m_audioAssetReference")]
        private AssetReference m_AudioAssetReference;

        public AudioMixerGroup MixerGroup => m_MixerGroup;

        [SerializeField]
        [FormerlySerializedAs("m_mixerGroup")]
        private AudioMixerGroup m_MixerGroup;

        public float Volume => m_Volume;

        [SerializeField, Range(0f, 1f)]
        [FormerlySerializedAs("m_volume")]
        private float m_Volume = 1.0f;

        public bool Loop => m_Loop;

        [SerializeField]
        [FormerlySerializedAs("m_loop")]
        private bool m_Loop = false;

        public bool ReleaseResourceOnStop => m_ReleaseResourceOnStop;

        [SerializeField, Tooltip("Should resource be unloaded from memory when stopped?")]
        [FormerlySerializedAs("m_releaseResourceOnStop")]
        private bool m_ReleaseResourceOnStop = true;
    }
}