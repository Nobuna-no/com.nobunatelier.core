using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/AI/AI Controller Module: Move To Target")]
    public class AIControllerMoveToTarget : AIControllerModuleBase
    {
        [SerializeField]
        private Transform m_target;

        private Character2DMovementVelocity m_movementModule;

        public void SetTarget(Transform target)
        {
            m_target = target;
        }

        public override bool IsAvailable()
        {
            return base.IsAvailable() && m_target != null;
        }

        public override void EnableAIModule()
        {
            ControlledCharacter.TryGetVelocityModule(out m_movementModule);
            if (m_movementModule == null)
            {
                Debug.LogError($"{this.name}: Character '{ControlledCharacter.name}' doesn't have a valid {typeof(Character2DMovementVelocity).Name}.");
                this.enabled = false;
            }
        }

        public override void DisableAIModule()
        { }

        public override void UpdateModule(float deltaTime)
        {
            Vector3 origin = ControlledCharacter.Position;
            Vector3 destination = m_target.position;
            Vector3 dir = destination - origin;

            m_movementModule.MoveInput(dir.normalized);
        }
    }
}