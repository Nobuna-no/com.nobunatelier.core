using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    // Represent
    public class AnimMontageDefinition : AnimSequenceDefinition
    {
        // public AnimSequenceDefinition AnimSequence => m_animSequence;
        public ParticleEffect Particle => m_Particle;

        public IReadOnlyList<SoundEffect> SoundEffects => m_SoundEffects;

        public bool HasFX => !string.IsNullOrEmpty(m_Particle.AssetReference.AssetGUID);

        public bool HasSFX => m_SoundEffects != null && m_SoundEffects.Length > 0;

        [Header("AnimMontage")]
        [SerializeField, FormerlySerializedAs("m_particle")]
        private ParticleEffect m_Particle;

        [SerializeField, FormerlySerializedAs("m_soundEffects")]
        private SoundEffect[] m_SoundEffects;

        [System.Serializable]
        public class ParticleEffect
        {
            public LoadableParticleSystem AssetReference => m_Particle;
            public float StartDelay => m_FxStartDelay;
            public Vector3 ParticleOffset => m_ParticleOffset;
            public Vector3 ParticleRotation => m_ParticleRotation;
            public Vector3 ParticleScale => m_ParticleScale;

            [SerializeField, FormerlySerializedAs("m_particle")]
            private LoadableParticleSystem m_Particle;

            [SerializeField, FormerlySerializedAs("m_fxStartDelay")]
            private float m_FxStartDelay;

            [SerializeField, Tooltip("Offset relative to the actor spawning the particle."), FormerlySerializedAs("m_particleOffset")]
            private Vector3 m_ParticleOffset = Vector3.zero;

            [SerializeField, FormerlySerializedAs("m_particleRotation")]
            private Vector3 m_ParticleRotation = Vector3.zero;

            [SerializeField, FormerlySerializedAs("m_particleScale")]
            private Vector3 m_ParticleScale = Vector3.one;
        }

        [System.Serializable]
        public class SoundEffect
        {
            public LoadableAudioSource AssetReference => m_AudioSource;
            public float StartDelay => m_SfxStartDelay;
            public Vector3 AudioOffset => m_AudioOffset;

            [SerializeField, FormerlySerializedAs("m_audioSource")]
            private LoadableAudioSource m_AudioSource;

            [SerializeField, FormerlySerializedAs("m_sfxStartDelay")]
            private float m_SfxStartDelay;

            [SerializeField, Tooltip("Offset relative to the actor spawning the audio."), FormerlySerializedAs("m_audioOffset")]
            private Vector3 m_AudioOffset = Vector3.zero;
        }
    }
}