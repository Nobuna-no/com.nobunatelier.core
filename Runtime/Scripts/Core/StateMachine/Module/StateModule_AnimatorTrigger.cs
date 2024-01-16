using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Trigger Animator Param")]
    public class StateModule_AnimatorTrigger : StateComponentModule
    {
        [SerializeField]
        private Animator m_animator;

        [SerializeField, AnimatorParam("m_animator")]
        private string m_animParamName;

        public override void Enter()
        {
            Debug.Assert(m_animator, $"No animator assigned!", this);
            m_animator.SetTrigger(m_animParamName);

            base.Enter();
        }
    }
}