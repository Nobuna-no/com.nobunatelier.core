using System;
using NobunAtelier.Gameplay;
using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// Spawns hitbox(es) that deal damage on contact.
    /// Hitboxes are position-tracked every frame while active.
    /// Impact VFX/SFX fire on each hit.
    /// Damage = HitDefinition.DamageAmount * context.Value (passed as damage multiplier).
    /// </summary>
    [Serializable]
    public class DamageAreaEffect : AbilityEffect
    {
        [Header("Hitbox")]
        [SerializeField] private AbilityLoadableHitbox[] m_Hitboxes;
        [SerializeField] private HitDefinition m_HitDefinition;
        [SerializeField] private TeamDefinition.Target m_HitTarget = TeamDefinition.Target.Enemies;

        [Header("Impact FX")]
        [Tooltip("Where impact FX spawn: Self (caster) or Target (hit entity).")]
        [SerializeField] private EffectTarget m_ImpactFXOrigin = EffectTarget.Target;
        [SerializeField] private AbilityLoadableParticleSystem[] m_ImpactVFX;
        [SerializeField] private AbilityLoadableAudioSource[] m_ImpactSFX;

        public override IAbilityEffectInstance CreateInstance(AbilityController controller)
        {
            return new Instance(this, controller);
        }

        private class Instance : IAbilityEffectInstance
        {
            private readonly DamageAreaEffect m_Data;
            private readonly AbilityController m_Controller;
            private AbilityLoadableHitboxFactory m_HitboxFactory;
            private AbilityLoadableVFXFactory m_ImpactVfxFactory;
            private AbilityLoadableSFXFactory m_ImpactSfxFactory;
            private Transform m_Target;
            private bool m_IsRegistered;
            private bool m_IsActive;

            public bool NeedsUpdate => m_IsActive;

            public Instance(DamageAreaEffect data, AbilityController controller)
            {
                m_Data = data;
                m_Controller = controller;

                if (data.m_Hitboxes != null && data.m_Hitboxes.Length > 0)
                {
                    m_HitboxFactory = new AbilityLoadableHitboxFactory(data.m_Hitboxes);
                    m_HitboxFactory.AsyncReleaseOnPlay = false;
                }

                if (data.m_ImpactVFX != null && data.m_ImpactVFX.Length > 0)
                    m_ImpactVfxFactory = new AbilityLoadableVFXFactory(data.m_ImpactVFX);

                if (data.m_ImpactSFX != null && data.m_ImpactSFX.Length > 0)
                    m_ImpactSfxFactory = new AbilityLoadableSFXFactory(data.m_ImpactSFX);
            }

            public void Execute(AbilityEffectContext context)
            {
                if (m_HitboxFactory == null)
                {
                    Debug.LogWarning("[DamageAreaEffect] No hitboxes configured.", m_Controller);
                    return;
                }

                if (!AbilityModuleUtility.TryGetTarget(m_Controller, context.Target, out m_Target))
                    return;

                Stop();

                if (!m_IsRegistered)
                {
                    m_HitboxFactory.RegisterResources();
                    m_ImpactVfxFactory?.RegisterResources();
                    m_ImpactSfxFactory?.RegisterResources();
                    m_IsRegistered = true;
                }

                // Setup hitboxes with damage and team info
                m_HitboxFactory.SetupHitboxes(
                    m_Target,
                    m_Data.m_HitTarget,
                    m_Controller.Team,
                    m_Data.m_HitDefinition);

                // Set damage multiplier from context value
                m_HitboxFactory.SetDamageMultiplier(context.Value);

                // Listen for hits to trigger impact FX
                m_HitboxFactory.AddListenerOnHit(OnHit);

                // Activate hitboxes
                m_HitboxFactory.PlayAll(m_Target);
                m_IsActive = true;
            }

            public void Update(float deltaTime)
            {
                if (m_IsActive && m_Target != null)
                {
                    m_HitboxFactory?.UpdateHitbox(m_Target);
                }
            }

            public void Stop()
            {
                if (m_IsActive)
                {
                    m_HitboxFactory?.ReleaseCachedHitboxes();
                    m_IsActive = false;
                }

                if (m_IsRegistered)
                {
                    m_HitboxFactory?.UnregisterResources();
                    m_ImpactVfxFactory?.UnregisterResources();
                    m_ImpactSfxFactory?.UnregisterResources();
                    m_IsRegistered = false;
                }
            }

            private void OnHit(HitInfo hitInfo)
            {
                if (!AbilityModuleUtility.TryGetTarget(m_Controller, m_Data.m_ImpactFXOrigin, out var fxTarget))
                    return;

                m_ImpactVfxFactory?.PlayAll(fxTarget);
                m_ImpactSfxFactory?.PlayAll(fxTarget);
            }
        }
    }
}
