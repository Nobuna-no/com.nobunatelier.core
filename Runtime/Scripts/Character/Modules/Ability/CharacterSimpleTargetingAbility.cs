using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Ability/AbilityModule: Simple Targeting")]
    public class CharacterSimpleTargetingAbility : CharacterAbilityModuleBase
    {
        [SerializeField]
        private bool m_canChangeTarget = true;

        public bool CanChangeTarget
        {
            get => m_canChangeTarget;
            set { m_canChangeTarget = value; }
        }

        public TargetChangedEvent OnTargetRefreshed;

        private ITargetable m_currentTarget;
        public ITargetable CurrentTarget => m_currentTarget;

        public void RefreshTarget()
        {
            // Init or when target died and is no longer targetable.
            if (m_currentTarget == null || !m_currentTarget.IsTargetable)
            {
                NextTarget();
            }

            OnTargetRefreshed?.Invoke(m_currentTarget);
        }

        public void NextTarget()
        {
            if (!TargetManager.IsSingletonValid)
            {
                Debug.LogWarning($"{this}: Trying to use targeting ability, but no TargetManager instance available.");
                return;
            }

            if (!m_canChangeTarget)
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

            if (m_currentTarget != null && closestTarget == null)
            {
                return;
            }

            // Update the current target with the closest one
            m_currentTarget = closestTarget;
            OnTargetRefreshed?.Invoke(m_currentTarget);
        }
    }
}
