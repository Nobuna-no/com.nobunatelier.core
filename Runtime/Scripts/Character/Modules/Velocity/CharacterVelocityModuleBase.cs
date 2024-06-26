using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterVelocityModuleBase : CharacterModuleBase
    {
        /// <summary>
        /// Last move direction sent by a Controller module.
        /// Need to be manually zeroed.
        /// </summary>
        public Vector3 LastMoveDirection { get; protected set; }

        public virtual void MoveInput(Vector3 direction)
        {
            LastMoveDirection = direction;
        }

        public virtual void StateUpdate(bool grounded)
        { }

        public abstract Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime);

        public virtual void OnModuleCollisionEnter(Collision collision)
        { }

        public virtual void OnModuleCollisionExit(Collision collision)
        { }

        public void OnModuleCollisionStay(Collision collision)
        { }
    }
}