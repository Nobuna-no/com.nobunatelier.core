using UnityEngine;

namespace NobunAtelier
{
    public class AnimationModule : CharacterAbilityModuleBase
    {
        [SerializeField]
        protected Animator m_animator;

        public Animator Animator => m_animator;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            if (m_animator != null)
            {
                return;
            }

            m_animator = character.GetComponentInChildren<Animator>();
            Debug.Assert(m_animator, $"{this.name}: no Animation found in {character.name} children.");
        }

        public virtual void AttachAnimator(Animator animator, bool destroyCurrent = true)
        {
            if (destroyCurrent && Animator)
            {
                Destroy(m_animator.gameObject);
            }

            m_animator = Instantiate(animator.gameObject, this.transform).GetComponent<Animator>();
        }

        public virtual void SetAnimationSpeed(float speed)
        {
            m_animator.speed = speed;
        }

        public virtual void ResetAnimationSpeed()
        {
            m_animator.speed = 1;
        }
    }
}