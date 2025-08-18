using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterPhysicsModule : MonoBehaviour
    {
        public enum VelocityApplicationUpdate
        {
            Update,
            FixedUpdate,
            Manual
        }

        public abstract VelocityApplicationUpdate VelocityUpdate { get; }
        public abstract Vector3 Position { get; set; }
        public abstract Vector3 Velocity { get; set; }
        public abstract Quaternion Rotation { get; set; }
        public bool IsGrounded => CanBeGrounded && CheckGroundedState();
        
        public bool CanBeGrounded { get; set; } = true;

        public Character ModuleOwner { get; private set; }

        public abstract void ApplyVelocity(Vector3 newVelocity, float deltaTime);

        public virtual void ModuleInit(Character character)
        {
            ModuleOwner = character;
        }

        public virtual void OnModuleCollisionEnter(Collision collision)
        { }

        public virtual void OnModuleCollisionExit(Collision collision)
        { }

        public virtual void OnModuleCollisionStay(Collision collision)
        { }

        protected abstract bool CheckGroundedState();
    }
}