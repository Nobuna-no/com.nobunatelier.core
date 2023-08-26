using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier
{
    public class AnimMontageDefinition : DataDefinition
    {
        public AnimSequenceDefinition AnimSequence => m_animSequence;
        public ParticleEffect Particle => m_particle;
        public IReadOnlyList<SoundEffect> SoundEffects => m_soundEffects;

        [SerializeField]
        AnimSequenceDefinition m_animSequence;

        [SerializeField]
        private ParticleEffect m_particle;

        [SerializeField]
        private SoundEffect[] m_soundEffects;

        public bool HasFX => !string.IsNullOrEmpty(m_particle.AssetReference.AssetGUID);

        public bool HasSFX => m_soundEffects != null && m_soundEffects.Length > 0;

        [System.Serializable]
        public class ParticleEffect
        {
            public AssetReferenceParticleSystem AssetReference => m_particle;
            public float StartDelay => m_fxStartDelay;

            [SerializeField]
            private AssetReferenceParticleSystem m_particle;

            [SerializeField]
            private float m_fxStartDelay;
        }

        [System.Serializable]
        public class SoundEffect
        {
            public AssetReferenceAudioSource AssetReference => m_audioSource;
            public float StartDelay => m_sfxStartDelay;

            [SerializeField]
            private AssetReferenceAudioSource m_audioSource;

            [SerializeField]
            private float m_sfxStartDelay;
        }
    }
}