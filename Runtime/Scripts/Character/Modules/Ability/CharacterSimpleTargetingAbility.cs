using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Ability/AbilityModule: Simple Targeting")]
    public class CharacterSimpleTargetingAbility : CharacterAbilityModuleBase
    {
        public bool CanChangeTarget = true;

        public TargetChangedEvent OnTargetChanged;

        private ITargetable m_currentTarget;
        public ITargetable CurrentTarget => m_currentTarget;

        public void RefreshTarget()
        {
            if (m_currentTarget == null)
            {
                return;
            }

            if (!m_currentTarget.IsTargetable)
            {
                NextTarget();
            }
        }

        public void NextTarget()
        {
            if (TargetManager.Instance == null)
            {
                Debug.LogError($"{this}: Trying to use targeting ability, but no TargetManager instance available.");
            }

            if (!CanChangeTarget)
            {
                return;
            }

            var targets = TargetManager.Instance.Targets;

            if (targets == null || targets.Count == 0)
            {
                // No targets available
                m_currentTarget = null;
                return;
            }

            // Find the closest target to the current target position
            float closestDistance = float.MaxValue;
            ITargetable closestTarget = null;
            for (int i = 0, c = targets.Count; i < c; i++)
            {
                ITargetable target = targets[i];
                if (!target.IsTargetable || (m_currentTarget != null && target == m_currentTarget))
                    continue;

                float distance = Vector3.Distance(ModuleOwner.Position, target.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            if(m_currentTarget != null && closestTarget == null)
            {
                return;
            }

            // Update the current target with the closest one
            m_currentTarget = closestTarget;
            OnTargetChanged?.Invoke(m_currentTarget);
        }
    }
}
