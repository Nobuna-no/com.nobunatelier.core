using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [DefaultExecutionOrder(0)]
    public class TargetManager : SingletonManager<TargetManager>
    {
        private List<ITargetable> m_targets = new List<ITargetable>();
        public List<ITargetable> Targets => m_targets;

        protected override TargetManager GetInstance()
        {
            return this;
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
        }
    }
}
