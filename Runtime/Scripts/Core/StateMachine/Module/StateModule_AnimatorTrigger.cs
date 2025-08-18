using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Trigger Animator Param")]
    public class StateModule_AnimatorTrigger : StateComponentModule
    {
        [SerializeField, FormerlySerializedAs("m_animator")]
        private Animator m_Animator;

        [SerializeField, AnimatorParam("m_Animator"), FormerlySerializedAs("m_animParamName")]
        private string m_AnimParamName;

        public override void Enter()
        {
            Debug.Assert(m_Animator, $"No animator assigned!", this);
            m_Animator.SetTrigger(m_AnimParamName);

            base.Enter();
        }
    }
}