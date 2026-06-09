using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Grants or revokes a gameplay tag when executed.
    /// Grant mode: Execute grants, Stop revokes (cleanup on teardown).
    /// Revoke mode: Execute revokes, Stop is no-op.
    /// Bind to gameplay events for precise timing control beyond phase boundaries.
    /// </summary>
    [Serializable]
    public class GameplayTagEffect : AbilityEffect
    {
        public enum Mode
        {
            Grant,
            Revoke
        }

        [SerializeField] private GameplayTagDefinition m_Tag;
        [SerializeField] private Mode m_Mode = Mode.Grant;

        public override IAbilityEffectInstance CreateInstance(AbilityController controller)
        {
            return new Instance(m_Tag, m_Mode, controller);
        }

        private class Instance : IAbilityEffectInstance
        {
            private readonly GameplayTagDefinition m_Tag;
            private readonly Mode m_Mode;
            private readonly AbilityController m_Controller;
            private GameplayTagModule m_TagModule;
            private bool m_IsGranted;

            public bool NeedsUpdate => false;

            public Instance(GameplayTagDefinition tag, Mode mode, AbilityController controller)
            {
                m_Tag = tag;
                m_Mode = mode;
                m_Controller = controller;
            }

            public void Execute(AbilityEffectContext context)
            {
                if (m_Tag == null)
                    return;

                m_TagModule ??= m_Controller.TagModule;
                if (m_TagModule == null)
                    return;

                if (m_Mode == Mode.Grant)
                {
                    m_TagModule.GrantTag(m_Tag);
                    m_IsGranted = true;
                }
                else
                {
                    m_TagModule.RevokeTag(m_Tag);
                }
            }

            public void Update(float deltaTime) { }

            public void Stop()
            {
                if (m_IsGranted && m_TagModule != null)
                {
                    m_TagModule.RevokeTag(m_Tag);
                    m_IsGranted = false;
                }
            }
        }
    }
}
