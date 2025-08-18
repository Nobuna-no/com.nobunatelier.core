using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AnimationModule : CharacterAbilityModuleBase
    {
        [SerializeField, FormerlySerializedAs("m_animator")]
        protected Animator m_Animator;

        public Animator Animator => m_Animator;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            if (m_Animator != null)
            {
                return;
            }

            m_Animator = character.GetComponentInChildren<Animator>();
            Debug.Assert(m_Animator, $"{this.name}: no Animation found in {character.name} children.");
        }

        public virtual void AttachAnimator(Animator animator, bool destroyCurrent = true)
        {
            if (destroyCurrent && Animator)
            {
                Destroy(m_Animator.gameObject);
            }

            m_Animator = Instantiate(animator.gameObject, this.transform).GetComponent<Animator>();
        }

        public virtual void SetAnimationSpeed(float speed)
        {
            m_Animator.speed = speed;
        }

        public virtual void ResetAnimationSpeed()
        {
            m_Animator.speed = 1;
        }

        public void AnimatorParameterSetTrigger(string name) => m_Animator.SetTrigger(name);
        public void AnimatorParameterResetTrigger(string name) => m_Animator.ResetTrigger(name);
        public void AnimatorParameterEnableBoolean(string name) => m_Animator.SetBool(name, true);
        public void AnimatorParameterDisableBoolean(string name) => m_Animator.SetBool(name, false);
    }
}