using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterVelocityModule : CharacterModuleBase
    {
        public Vector3 LastMoveDirection { get; protected set; }

        public virtual void MoveInput(Vector3 direction)
        {
            LastMoveDirection = direction;
        }

        public abstract Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime);

        public virtual bool StopVelocityUpdate()
        {
            return false;
        }

        public virtual void OnModuleCollisionEnter(Collision collision) { }

        public virtual void OnModuleCollisionExit(Collision collision) { }

        public void OnModuleCollisionStay(Collision collision) { }
    }
}