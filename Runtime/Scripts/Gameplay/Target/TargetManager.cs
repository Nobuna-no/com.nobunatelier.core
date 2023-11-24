using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [DefaultExecutionOrder(0)]
    public class TargetManager : Singleton<TargetManager>
    {
        [SerializeField]
        private CharacterSimpleTargetingAbility m_targetingModule;
        private List<ITargetable> m_targets = new List<ITargetable>();
        public List<ITargetable> Targets => m_targets;

        private void Start()
        {
            if (!m_targetingModule)
            {
                m_targetingModule = FindAnyObjectByType<CharacterSimpleTargetingAbility>();
            }

            Debug.Assert(m_targetingModule != null);
        }

        public void Register(ITargetable target)
        {
            if (m_targets.Contains(target))
            {
                return;
            }

            m_targets.Add(target);
        }

        public void Unregister(ITargetable target)
        {
            if (!m_targets.Contains(target))
            {
                return;
            }

            m_targets.Remove(target);

            if (m_targetingModule)
            {
                m_targetingModule.RefreshTarget();
            }
        }
    }
}
