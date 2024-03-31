using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    // Represent
    public class AnimMontageDefinition : AnimSequenceDefinition
    {
        // public AnimSequenceDefinition AnimSequence => m_animSequence;
        public ParticleEffect Particle => m_particle;

        public IReadOnlyList<SoundEffect> SoundEffects => m_soundEffects;

        public bool HasFX => !string.IsNullOrEmpty(m_particle.AssetReference.AssetGUID);

        public bool HasSFX => m_soundEffects != null && m_soundEffects.Length > 0;

        [Header("AnimMontage")]
        [SerializeField]
        private ParticleEffect m_particle;

        [SerializeField]
        private SoundEffect[] m_soundEffects;

        [System.Serializable]
        public class ParticleEffect
        {
            public LoadableParticleSystem AssetReference => m_particle;
            public float StartDelay => m_fxStartDelay;
            public Vector3 ParticleOffset => m_particleOffset;
            public Vector3 ParticleRotation => m_particleRotation;
            public Vector3 ParticleScale => m_particleScale;

            [SerializeField]
            private LoadableParticleSystem m_particle;

            [SerializeField]
            private float m_fxStartDelay;

            [SerializeField, Tooltip("Offset relative to the actor spawning the particle.")]
            private Vector3 m_particleOffset = Vector3.zero;

            [SerializeField]
            private Vector3 m_particleRotation = Vector3.zero;

            [SerializeField]
            private Vector3 m_particleScale = Vector3.one;
        }

        [System.Serializable]
        public class SoundEffect
        {
            public LoadableAudioSource AssetReference => m_audioSource;
            public float StartDelay => m_sfxStartDelay;
            public Vector3 AudioOffset => m_audioOffset;

            [SerializeField]
            private LoadableAudioSource m_audioSource;

            [SerializeField]
            private float m_sfxStartDelay;

            [SerializeField, Tooltip("Offset relative to the actor spawning the audio.")]
            private Vector3 m_audioOffset = Vector3.zero;
        }
    }
}