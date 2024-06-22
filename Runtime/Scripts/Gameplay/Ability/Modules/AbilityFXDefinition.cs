using NobunAtelier;
using UnityEngine;

public class AbilityFXDefinition : AbilityModuleDefinition
{
    [SerializeField] private EffectTarget m_target;
    [SerializeField] private AbilityLoadableParticleSystem[] m_VFX;
    [SerializeField] private AbilityLoadableAudioSource[] m_SFX;

    public override IAbilityModuleInstance CreateInstance(AbilityController controller)
    {
        return new Instance(controller, this);
    }

    public class Instance : AbilityModuleInstance<AbilityFXDefinition>
    {
        public override bool RunUpdate => false;

        private AbilityLoadableSFXFactory m_sfxFactory;
        private AbilityLoadableVFXFactory m_vfxFactory;
        private Transform m_target;
        private bool m_isRegistered;

        public Instance(AbilityController controller, AbilityFXDefinition data)
            : base(controller, data)
        {
            m_sfxFactory = new AbilityLoadableSFXFactory(Data.m_SFX);
            m_vfxFactory = new AbilityLoadableVFXFactory(Data.m_VFX);
            m_isRegistered = false;
        }

        public override void InitiateExecution()
        {
            AbilityModuleHelper.TryGetTarget(Controller, Data.m_target, out m_target);
            Stop();
            m_sfxFactory.RegisterResources();
            m_vfxFactory.RegisterResources();
            m_isRegistered = true;
        }

        public override void ExecuteEffect()
        {
            if (!m_isRegistered)
            {
                InitiateExecution();
            }

            // Debug.Log($"[{Time.frameCount}]Playing {Data}...");
            m_sfxFactory.PlayAll(m_target);
            m_vfxFactory.PlayAll(m_target);
        }

        public override void Stop()
        {
            m_sfxFactory.UnregisterResources();
            m_vfxFactory.UnregisterResources();
            m_isRegistered = false;
        }
    }
}