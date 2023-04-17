using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterBodyModuleBase : MonoBehaviour
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
        public abstract bool IsGrounded { get; }

        public AtelierCharacter ModuleOwner { get; private set; }

        public abstract void ApplyVelocity(Vector3 newVelocity, float deltaTime);

        public virtual void ModuleInit(AtelierCharacter character)
        {
            ModuleOwner = character;
        }

        public virtual void OnModuleCollisionEnter(Collision collision) { }

        public virtual void OnModuleCollisionExit(Collision collision) { }

        public virtual void OnModuleCollisionStay(Collision collision) { }
    }
}