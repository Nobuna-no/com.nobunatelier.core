using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class MovementAnimationModule : AnimationModule
    {
        [SerializeField, AnimatorParam("m_animator")]
        private string m_moveSpeedFloatName;

        protected override void OnAbilityUpdate(float deltaTime)
        {
            var movement = ModuleOwner.GetMoveVector();
            movement.y = 0;

            Animator.SetFloat(m_moveSpeedFloatName, movement.sqrMagnitude);
        }
    }
}