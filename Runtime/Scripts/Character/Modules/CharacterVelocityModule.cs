using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterVelocityModule : CharacterModuleBase
    {
        public Vector2 LastMoveInput { get; protected set; }

        public virtual void MoveInput(Vector2 input)
        {
            LastMoveInput = input;
        }

        public abstract Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime);
    }
}