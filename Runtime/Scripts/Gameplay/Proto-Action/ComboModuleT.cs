using NaughtyAttributes;
using NobunAtelier.Gameplay;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [RequireComponent(typeof(TeamModule))]
    public abstract class ComboModuleT<T, ComboAttackT> : CharacterAbilityModuleBase
        where T : AttackDefinition
        where ComboAttackT : ComboModuleT<T, ComboAttackT>.ComboAttack
    {
        public IReadOnlyList<Combo> ComboCollection => m_comboCollection;
        public Combo ActiveCombo => m_activeCombo;
        public ComboState State => m_comboState;
        public ComboAttackT CurrentAttack => m_currentAttack;
        public int AttackIndex => m_comboIndex;

        [SerializeField]
        private Combo[] m_comboCollection;

        public UnityEvent OnAttackFollowUp;

        [SerializeField, ReadOnly]
        private ComboState m_comboState = ComboState.ReadyToAttack;

        [SerializeField]
        private bool m_debugLog = false;

        private Queue<System.Action> m_actionsQueue = new Queue<System.Action>();
        private Combo m_activeCombo;
        private ComboAttackT m_currentAttack;
        private TeamModule m_teamModule;
        private int m_comboIndex;
        private bool m_canExecuteNewAction = true;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            m_comboIndex = 0;

            Debug.Assert(ModuleOwner.TryGetAbilityModule(out m_teamModule), $"{this.name}: Owner need to be part of a team!", this);
        }

        public override void ModuleStop()
        {
            base.ModuleStop();

            if (ActiveCombo != null)
            {
                foreach (var atk in ActiveCombo.Attacks)
                {
                    atk.ResourcesRelease();
                }
            }
        }

        public override void AbilityUpdate(float deltaTime)
        {
            base.AbilityUpdate(deltaTime);

            if (m_comboState == ComboState.Attacking && CurrentAttack != null)
            {
                CurrentAttack.UpdateHitbox(ModuleOwner.Transform);
            }

            if (!m_canExecuteNewAction || m_actionsQueue.Count == 0)
            {
                return;
            }

            if (m_debugLog)
            {
                Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: Dequeue next attack.");
            }

            m_actionsQueue.Dequeue().Invoke();
            m_canExecuteNewAction = false;
        }

        public virtual void DoAllComboAttacks()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (m_comboCollection.Length == 0)
            {
                Debug.LogWarning($"{this.name}: No combo assigned.");
                return;
            }

            if (m_activeCombo == null)
            {
                Debug.LogWarning($"{this.name}: No active combo assigned. Call 'SetActiveCombo' before");
                return;
            }

            for (int i = m_activeCombo.Attacks.Count - 1; i >= 0; --i)
            {
                m_actionsQueue.Enqueue(() =>
                {
                    DoComboInternal();
                });
            }
        }

        public virtual void DoNextComboAttack()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (m_comboCollection.Length == 0)
            {
                Debug.LogWarning($"{this.name}: No combo assigned.");
                return;
            }

            if (m_activeCombo == null)
            {
                Debug.LogWarning($"{this.name}: No active combo assigned. Call 'SetActiveCombo' before");
                return;
            }

            // Cache the combo action in a queue, later we can improve the queue with a TimingQueue.
            m_actionsQueue.Enqueue(() =>
            {
                DoComboInternal();
            });
        }

        public void PickNewRandomCombo()
        {
            int comboIndex = UnityEngine.Random.Range(0, m_comboCollection.Length);
            SetActiveCombo(m_comboCollection[comboIndex].ComboName);
        }

        public virtual void SetActiveCombo(string comboName)
        {
            if (ActiveCombo != null)
            {
                foreach (var atk in ActiveCombo.Attacks)
                {
                    atk.ResourcesRelease();
                }
            }

            StopCombo();

            m_activeCombo = null;

            for (int i = m_comboCollection.Length - 1; i >= 0; i--)
            {
                if (comboName == m_comboCollection[i].ComboName)
                {
                    m_activeCombo = m_comboCollection[i];
                    break;
                }
            }

            if (ActiveCombo != null)
            {
                foreach (var atk in ActiveCombo.Attacks)
                {
                    atk.ResourcesInit();
                }
            }

            m_canExecuteNewAction = m_activeCombo != null;
        }

        // Break the combo and reset. To use after taking a damage for instance.
        public virtual void StopCombo()
        {
            if (m_activeCombo != null && m_activeCombo.Attacks[m_comboIndex].Hitbox != null)
            {
                CurrentAttack.Hitbox.HitEnd();
                CurrentAttack.Hitbox.OnHit.RemoveListener(OnAttackHitImpl);
            }

            if (m_debugLog)
            {
                Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: StopCombo.");
            }

            m_comboIndex = 0;
            m_comboState = ComboState.ReadyToAttack;
        }

        protected abstract void FollowUpAttackImpl(int previousAttackIndex);

        protected abstract void InitialAttackImpl();

        protected virtual void OnAttackHitImpl(HitInfo hitInfo)
        {
            if (CurrentAttack == null)
            {
                return;
            }

            // good enough for now
            CurrentAttack.SetupImpactParticleAndPlay(ModuleOwner.transform);
            CurrentAttack.ImpactParticle.transform.position = hitInfo.ImpactLocation;
        }

        protected abstract void SetupAttackImpl();

        // Too late to follow up
        protected virtual void AttackEnd(AnimSequenceDefinition.Segment animSegment)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (m_debugLog)
            {
                Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: AttackEnd.");
            }

            m_comboState = ComboState.ReadyToAttack;
            // m_animationModule.ResetAnimationSpeed();

            m_canExecuteNewAction = true;

            // RemoveListenersFromAnimSegments();
        }

        protected virtual void AttackWarningBegin(AnimSequenceDefinition.Segment animSegment)
        {
            var hitbox = m_activeCombo.Attacks[m_comboIndex].Hitbox as HitboxWithIndicatorBehaviour;
            if (hitbox)
            {
                hitbox.StartWarningAnimation(animSegment.Duration);
            }
        }

        protected virtual void AttackHitBegin(AnimSequenceDefinition.Segment animSegment)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (m_debugLog)
            {
                Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: AttackHitStart.");
            }

            m_activeCombo.Attacks[m_comboIndex].Hitbox.HitBegin();
        }

        // Ready to follow up
        protected virtual void AttackHitEnd(AnimSequenceDefinition.Segment animSegment)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (m_debugLog)
            {
                Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: AttackHitEnd.");
            }

            if (m_activeCombo != null && m_activeCombo.Attacks[m_comboIndex].Hitbox != null)
            {
                m_activeCombo.Attacks[m_comboIndex].Hitbox.HitEnd();
                m_activeCombo.Attacks[m_comboIndex].Hitbox.OnHit.RemoveListener(OnAttackHitImpl);
            }

            m_comboState = ComboState.FollowUpWindow;
            m_canExecuteNewAction = true;
        }

        private void DoComboInternal()
        {
            switch (m_comboState)
            {
                case ComboState.Attacking:
                    // ignore
                    break;

                case ComboState.ReadyToAttack:
                    {
                        if (m_debugLog)
                        {
                            Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: DoComboInternal - First Attack.");
                        }

                        m_comboIndex = 0;
                        m_currentAttack = m_activeCombo.Attacks[0];

                        // m_comboCancelled = false;
                        InitialAttackImpl();
                        // m_animationModule.PlayAnimSequence(m_activeCombo.Attacks[0].AnimData);
                        //
                        // AddListenersToAnimSegments();

                        SetupComboAttack();
                    }
                    break;

                case ComboState.FollowUpWindow:
                    {
                        if (m_debugLog)
                        {
                            Debug.Log($"{this.name}{typeof(AnimComboModule).Name}: DoComboInternal - FollowUp Attack.");
                        }

                        // RemoveListenersFromAnimSegments();

                        int newIndex = (int)Mathf.Repeat(m_comboIndex + 1, m_activeCombo.Attacks.Count);
                        m_currentAttack = m_activeCombo.Attacks[newIndex];
                        // m_animationModule.PlayAnimSequence(m_activeCombo.Attacks[m_comboIndex].AnimData);
                        FollowUpAttackImpl(m_comboIndex);
                        m_comboIndex = newIndex;

                        // AddListenersToAnimSegments();
                        SetupComboAttack();
                        OnAttackFollowUp?.Invoke();
                    }
                    break;
            }
        }

        private void SetupComboAttack()
        {
            m_comboState = ComboState.Attacking;

            m_activeCombo.Attacks[m_comboIndex].Hitbox.OnHit.AddListener(OnAttackHitImpl);
            m_activeCombo.Attacks[m_comboIndex].SetupHitbox(ModuleOwner.Transform, m_teamModule);

            SetupAttackImpl();
        }

        public enum ComboState
        {
            ReadyToAttack,
            Attacking, // State during which hitbox is active, can't attack
            FollowUpWindow // State after the hitbox is disabled, can do next attack of the combo
        }

        [System.Serializable]
        public class ComboAttack
        {
            public T AttackDefinition => m_attackDefinition;
            public HitDefinition Hit => m_attackDefinition.Hit;
            public HitboxBehaviour Hitbox => m_hitbox;

            [SerializeField]
            private T m_attackDefinition;

            private HitboxBehaviour m_hitbox;
            public ParticleSystem ImpactParticle { get; private set; } = null;

            public virtual void ResourcesInit()
            {
                if (m_attackDefinition == null)
                {
                    return;
                }

                HitboxInit();

                if (string.IsNullOrEmpty(m_attackDefinition.ImpactParticleReference.AssetGUID))
                {
                    return;
                }

                ImpactParticle = AtelierFactoryParticleSystemReference.GetProduct(m_attackDefinition.ImpactParticleReference);
            }

            public virtual void ResourcesRelease()
            {
                HitboxRelease();

                if (ImpactParticle == null)
                {
                    return;
                }

                AtelierFactoryParticleSystemReference.ReleaseProduct(m_attackDefinition.ImpactParticleReference, ImpactParticle);
                ImpactParticle = null;
            }

            public void SetupHitbox(Transform origin, TeamModule teamModule)
            {
                m_hitbox.SetHitDefinition(Hit);
                m_hitbox.SetTargetDefinition(m_attackDefinition.HitTarget);
                m_hitbox.SetOwner(teamModule);

                m_hitbox.transform.localPosition = origin.position + origin.TransformDirection(AttackDefinition.HitboxOffset);
                m_hitbox.transform.localRotation = origin.rotation * Quaternion.Euler(AttackDefinition.HitboxRotation);
                m_hitbox.transform.localScale = AttackDefinition.HitboxScale;
            }

            public void UpdateHitbox(Transform origin)
            {
                m_hitbox.transform.localPosition = origin.position + origin.TransformDirection(AttackDefinition.HitboxOffset);
                m_hitbox.transform.localRotation = origin.rotation * Quaternion.Euler(AttackDefinition.HitboxRotation);
            }

            public void SetupImpactParticleAndPlay(Transform origin)
            {
                if (ImpactParticle == null)
                {
                    return;
                }

                ImpactParticle.transform.position = origin.position + origin.TransformDirection(m_attackDefinition.ParticleOffset);
                ImpactParticle.transform.rotation = origin.rotation * Quaternion.Euler(m_attackDefinition.ParticleRotation);
                ImpactParticle.transform.localScale = m_attackDefinition.ParticleScale;
                ImpactParticle.Play(true);
            }

            private void HitboxInit()
            {
                if (string.IsNullOrEmpty(m_attackDefinition.HitboxReference.AssetGUID))
                {
                    return;
                }

                m_hitbox = AtelierFactoryHitboxBehaviourReference.GetProduct(m_attackDefinition.HitboxReference);
            }

            private void HitboxRelease()
            {
                if (m_hitbox == null)
                {
                    return;
                }

                AtelierFactoryHitboxBehaviourReference.ReleaseProduct(m_attackDefinition.HitboxReference, m_hitbox);
                m_hitbox = null;
            }
        }

        [System.Serializable]
        public class Combo
        {
            [SerializeField]
            private string m_comboName;

            [SerializeField]
            private ComboAttackT[] m_combo;

            public IReadOnlyList<ComboAttackT> Attacks => m_combo;
            public string ComboName => m_comboName;
        }
    }
}