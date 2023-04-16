using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterMovementModule : MonoBehaviour
    {
        //public override void Move()
        //{

        //}

        //protected virtual Vector3 ModuleUpdate()
        //{

        //}

        public virtual Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            return currentVelocity;
        }
    }
}