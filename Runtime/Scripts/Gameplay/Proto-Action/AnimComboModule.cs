using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NobunAtelier.AnimComboModule;

namespace NobunAtelier
{
    public class AnimComboModule : ComboModule<AttackAnimationDefinition, AnimComboAttack>
    {
        [SerializeField]
        private AnimationModule_AnimSequence m_animationModule;

        [SerializeField]
        private AnimSegmentDefinition m_attackWarningBeginSegmentDefinition;

        [SerializeField]
        private AnimSegmentDefinition m_attackHitBeginSegmentDefinition;

        [SerializeField]
        private AnimSegmentDefinition m_attackHitEndSegmentDefinition;

        [SerializeField]
        private AnimSegmentDefinition m_attackEndSegmentDefinition;

        private bool m_comboCancelled = false;

        private AnimationSegmentEvent m_currentAttackWarningBeginEvent;
        private AnimationSegmentEvent m_currentAttackHitBeginEvent;
        private AnimationSegmentEvent m_currentAttackHitEndEvent;
        private AnimationSegmentEvent m_currentAttackEndEvent;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(ModuleOwner.TryGetAbilityModule(out m_animationModule), $"{this.name}: No animation module found in character {ModuleOwner.name}.", this);
        }

        public override void StopCombo()
        {
            base.StopCombo();

            if (CurrentAttack != null && State == ComboState.Attacking && CurrentAttack.HasFX)
            {
                CurrentAttack.Particle.Stop();
            }

            m_comboCancelled = true;
            RemoveListenersFromAnimSegments();
        }

        protected override void AttackEnd(AnimSequenceDefinition.Segment animSegment)
        {
            base.AttackEnd(animSegment);

            m_animationModule.ResetAnimationSpeed();
            RemoveListenersFromAnimSegments();
        }

        protected override void InitialAttackImpl()
        {
            m_comboCancelled = false;

            m_animationModule.PlayAnimSequence(CurrentAttack.AnimData);

            AddListenersToAnimSegments();
        }

        protected override void FollowUpAttackImpl(int previousAttackIndex)
        {
            RemoveListenersFromAnimSegments();
            m_animationModule.PlayAnimSequence(CurrentAttack.AnimData);
            AddListenersToAnimSegments();
        }

        protected override void SetupAttackImpl()
        {
            if (CurrentAttack.HasFX)
            {
                // Foreach particles definition, check delay
                // If no delay, CurrentAttack.VFXPlay
                // If delay, start coroutine with particle definition

                if (CurrentAttack.ParticleStartDelay == 0)
                {
                    CurrentAttack.SetupParticleAndPlay(CurrentTarget);
                }
                else
                {
                    StartCoroutine(StartParticle_Coroutine(AttackIndex));
                }
            }

            if (CurrentAttack.HasSFX)
            {
                foreach (var sfx in CurrentAttack.SFXs)
                {
                    if (sfx.StartDelay == 0)
                    {
                        PlayAudio(sfx.AssetReference);
                        // AudioManager.Instance.Play3DAudio(sfx.AudioDefinition, CurrentAttack.ParticleTransform != null ? CurrentAttack.ParticleTransform : transform);
                    }
                    else
                    {
                        StartCoroutine(Start3DAudio_Coroutine(sfx.AssetReference, sfx.StartDelay));
                    }
                }
            }
        }

        private void PlayAudio(LoadableAudioSource assetReference)
        {
            CurrentAttack.PlayAudio(CurrentTarget.position, assetReference);

            //var audioSource = CurrentAttack.GetAudio(assetReference);
            //if (audioSource == null)
            //{
            //    return;
            //}

            //audioSource.transform.parent = CurrentAttack.ParticleTransform ? CurrentAttack.ParticleTransform : transform;
            //audioSource.transform.localPosition = Vector3.zero;

            //audioSource.Play();
        }

        private IEnumerator StartParticle_Coroutine(int index)
        {
            ActiveCombo.Attacks[index].Particle.Stop();
            yield return new WaitForSeconds(ActiveCombo.Attacks[index].ParticleStartDelay);

            if (m_comboCancelled)
            {
                yield break;
            }

            ActiveCombo.Attacks[index].SetupParticleAndPlay(CurrentTarget);

            //if (ActiveCombo.Attacks[index].ParticleTransform != null)
            //{
            //    ActiveCombo.Attacks[index].Particle.transform.position = ActiveCombo.Attacks[index].ParticleTransform.position;
            //    ActiveCombo.Attacks[index].Particle.transform.rotation = ActiveCombo.Attacks[index].ParticleTransform.rotation;
            //    ActiveCombo.Attacks[index].Particle.transform.localScale = ActiveCombo.Attacks[index].ParticleTransform.localScale;

            //    // ActiveCombo.Attacks[index].ImpactParticle.transform.position = ActiveCombo.Attacks[index].ParticleTransform.position;
            //    // ActiveCombo.Attacks[index].ImpactParticle.transform.rotation = ActiveCombo.Attacks[index].ParticleTransform.rotation;
            //    // ActiveCombo.Attacks[index].ImpactParticle.transform.localScale = ActiveCombo.Attacks[index].ParticleTransform.localScale;
            //}
        }

        private IEnumerator Start3DAudio_Coroutine(LoadableAudioSource assetReference, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (m_comboCancelled)
            {
                yield break;
            }

            PlayAudio(assetReference);
        }

        private void AddListenersToAnimSegments()
        {
            if (m_animationModule.TryGetAnimationEventForSegment(m_attackHitBeginSegmentDefinition, out m_currentAttackHitBeginEvent))
            {
                m_currentAttackHitBeginEvent.AddListener(AttackHitBegin);
            }
            if (m_animationModule.TryGetAnimationEventForSegment(m_attackHitEndSegmentDefinition, out m_currentAttackHitEndEvent))
            {
                m_currentAttackHitEndEvent.AddListener(AttackHitEnd);
            }
            if (m_animationModule.TryGetAnimationEventForSegment(m_attackEndSegmentDefinition, out m_currentAttackEndEvent))
            {
                m_currentAttackEndEvent.AddListener(AttackEnd);
            }
            if (m_animationModule.TryGetAnimationEventForSegment(m_attackWarningBeginSegmentDefinition, out m_currentAttackWarningBeginEvent))
            {
                m_currentAttackWarningBeginEvent.AddListener(AttackWarningBegin);
            }
        }

        private void RemoveListenersFromAnimSegments()
        {
            if (m_currentAttackHitBeginEvent != null)
            {
                m_currentAttackHitBeginEvent.RemoveListener(AttackHitBegin);
                m_currentAttackHitBeginEvent = null;
            }
            if (m_currentAttackHitEndEvent != null)
            {
                m_currentAttackHitEndEvent.RemoveListener(AttackHitEnd);
                m_currentAttackHitEndEvent = null;
            }
            if (m_currentAttackEndEvent != null)
            {
                m_currentAttackEndEvent.RemoveListener(AttackEnd);
                m_currentAttackEndEvent = null;
            }
            if (m_currentAttackWarningBeginEvent != null)
            {
                m_currentAttackWarningBeginEvent.RemoveListener(AttackWarningBegin);
                m_currentAttackWarningBeginEvent = null;
            }
        }

        [System.Serializable]
        public class AnimComboAttack : ComboAttack
        {
            public AnimMontageDefinition AnimData => AttackDefinition.AnimMontage;
            public ParticleSystem Particle { get; private set; } = null;
            public IReadOnlyList<AnimMontageDefinition.SoundEffect> SFXs => AnimData.SoundEffects;

            public float ParticleStartDelay => AnimData.Particle.StartDelay;

            public bool HasFX => Particle != null;
            public bool HasSFX => m_isSFXLoaded;

            private bool m_isSFXLoaded = false;

            private Dictionary<LoadableAudioSource, AudioSource> m_soundEffectsMap;

            public override void ResourcesInit()
            {
                base.ResourcesInit();
                VFXInit();
                SFXInit();
            }

            public override void ResourcesRelease()
            {
                base.ResourcesRelease();
                VFXRelease();
                SFXRelease();
            }

            private AudioSource GetAudio(LoadableAudioSource key)
            {
                if (m_soundEffectsMap == null || m_soundEffectsMap.Count == 0)
                {
                    return null;
                }

                if (m_soundEffectsMap.ContainsKey(key))
                {
                    return m_soundEffectsMap[key];
                }

                return null;
            }

            public void PlayAudio(Vector3 worldPosition, LoadableAudioSource assetReference)
            {
                var audioSource = GetAudio(assetReference);
                if (audioSource == null)
                {
                    return;
                }

                audioSource.transform.position = worldPosition;
                // audioSource.transform.localPosition = Vector3.zero;

                audioSource.Play();
            }

            public void SetupParticleAndPlay(Transform origin)
            {
                if (Particle == null)
                {
                    return;
                }

                Particle.transform.position = origin.position + origin.TransformDirection(AnimData.Particle.ParticleOffset);
                Particle.transform.rotation = origin.rotation * Quaternion.Euler(AnimData.Particle.ParticleRotation);
                Particle.transform.localScale = AnimData.Particle.ParticleScale;
                Particle.Play(true);
            }

            private void VFXInit()
            {
                if (AnimData == null || !AnimData.HasFX)
                {
                    return;
                }

                Particle = LoadableParticleSystemPoolFactory.Get(AnimData.Particle.AssetReference);
            }

            private void VFXRelease()
            {
                if (AnimData == null || !AnimData.HasFX || Particle == null)
                {
                    return;
                }

                LoadableParticleSystemPoolFactory.Release(AnimData.Particle.AssetReference, Particle);
                Particle = null;
            }

            private void SFXInit()
            {
                if (AnimData == null || !AnimData.HasSFX)
                {
                    return;
                }

                m_soundEffectsMap = new Dictionary<LoadableAudioSource, AudioSource>(AnimData.SoundEffects.Count);

                foreach (var se in AnimData.SoundEffects)
                {
                    if (m_soundEffectsMap.ContainsKey(se.AssetReference))
                    {
                        continue;
                    }

                    m_soundEffectsMap.Add(se.AssetReference, LoadableAudioSourcePoolFactory.Get(se.AssetReference));
                }
                m_isSFXLoaded = true;
            }

            // Release AudioSource but conserve the AssetReference keys.
            private void SFXRelease()
            {
                if (AnimData == null || !AnimData.HasSFX || m_soundEffectsMap == null || m_soundEffectsMap.Count == 0)
                {
                    return;
                }

                foreach (var se in m_soundEffectsMap)
                {
                    LoadableAudioSourcePoolFactory.Release(se.Key, se.Value);
                }

                m_soundEffectsMap.Clear();
            }
        }
    }
}