using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AudioStitcherDefinition : AudioResourceDefinition
    {
        public AudioDescription[] StitchedAudios => m_StitchedAudios;

        [SerializeField]
        [FormerlySerializedAs("m_stitchedAudios")]
        private AudioDescription[] m_StitchedAudios;

        [System.Serializable]
        public class AudioDescription
        {
            public AudioDefinition AudioDefinition => m_AudioDefinition;

            [SerializeField]
            [FormerlySerializedAs("m_audioDefinition")]
            private AudioDefinition m_AudioDefinition;

            public double Delay => m_DelayInSeconds;

            [SerializeField]
            [FormerlySerializedAs("m_delayInSeconds")]
            private double m_DelayInSeconds = -1;
        }
    }
}