using UnityEngine;

namespace NobunAtelier
{
    public class AudioStitcherDefinition : AudioResourceDefinition
    {
        public AudioDescription[] StitchedAudios => m_stitchedAudios;

        [SerializeField]
        private AudioDescription[] m_stitchedAudios;

        [System.Serializable]
        public class AudioDescription
        {
            public AudioDefinition AudioDefinition => m_audioDefinition;

            [SerializeField]
            private AudioDefinition m_audioDefinition;

            public double Delay => m_delayInSeconds;

            [SerializeField]
            private double m_delayInSeconds = -1;
        }
    }
}