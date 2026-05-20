using System;
using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// Fire-and-forget VFX/SFX effect.
    /// Spawns particles and audio at the resolved target position on Execute.
    /// </summary>
    [Serializable]
    public class FeedbackEffect : AbilityEffect
    {
        [SerializeField] private AbilityLoadableParticleSystem[] m_VFX;
        [SerializeField] private AbilityLoadableAudioSource[] m_SFX;

        public override IAbilityEffectInstance CreateInstance(AbilityController controller)
        {
            return new Instance(this, controller);
        }

        private class Instance : IAbilityEffectInstance
        {
            private readonly FeedbackEffect m_Data;
            private readonly AbilityController m_Controller;
            private AbilityLoadableVFXFactory m_VfxFactory;
            private AbilityLoadableSFXFactory m_SfxFactory;
            private bool m_IsRegistered;

            public bool NeedsUpdate => false;

            public Instance(FeedbackEffect data, AbilityController controller)
            {
                m_Data = data;
                m_Controller = controller;

                if (data.m_VFX != null && data.m_VFX.Length > 0)
                    m_VfxFactory = new AbilityLoadableVFXFactory(data.m_VFX);

                if (data.m_SFX != null && data.m_SFX.Length > 0)
                    m_SfxFactory = new AbilityLoadableSFXFactory(data.m_SFX);
            }

            public void Execute(AbilityEffectContext context)
            {
                if (m_VfxFactory == null && m_SfxFactory == null)
                {
                    Debug.LogWarning("[FeedbackEffect] No VFX or SFX configured.", m_Controller);
                    return;
                }

                if (!AbilityModuleUtility.TryGetTarget(m_Controller, context.Target, out var target))
                    return;

                if (!m_IsRegistered)
                {
                    m_VfxFactory?.RegisterResources();
                    m_SfxFactory?.RegisterResources();
                    m_IsRegistered = true;
                }

                m_VfxFactory?.PlayAll(target);
                m_SfxFactory?.PlayAll(target);
            }

            public void Update(float deltaTime) { }

            public void Stop()
            {
                if (m_IsRegistered)
                {
                    m_VfxFactory?.UnregisterResources();
                    m_SfxFactory?.UnregisterResources();
                    m_IsRegistered = false;
                }
            }
        }
    }
}
