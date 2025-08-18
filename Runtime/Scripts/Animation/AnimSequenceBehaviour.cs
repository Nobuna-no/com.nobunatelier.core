using NaughtyAttributes;
using NobunAtelier;
using UnityEngine;
using UnityEngine.Serialization;

public class AnimSequenceBehaviour : StateMachineBehaviour
{
    [InfoBox("Animator's State for Ability animations needs to have their transition's " +
        "Interuption Source set to `NextState` - Otherwise you might encounter issue " +
        "when trying to chain ability animation or chaining start chain using trigger animator param.",
        EInfoBoxType.Warning)]
    [SerializeField, FormerlySerializedAs("m_animSegmentEnterDefinition")]
    private AnimSegmentDefinition m_AnimSegmentEnterDefinition;

    [SerializeField, FormerlySerializedAs("m_animSegmentExitDefinition")]
    private AnimSegmentDefinition m_AnimSegmentExitDefinition;

    private AnimSequenceController animSequenceController;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!m_AnimSegmentEnterDefinition || !RefreshAnimSequenceController(animator, stateInfo))
        {
            return;
        }

        animSequenceController.OnAnimationSegmentTrigger(m_AnimSegmentEnterDefinition);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!m_AnimSegmentExitDefinition || !RefreshAnimSequenceController(animator, stateInfo))
        {
            return;
        }

        animSequenceController.OnAnimationSegmentTrigger(m_AnimSegmentExitDefinition);
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)

    private bool RefreshAnimSequenceController(Animator animator, AnimatorStateInfo stateInfo)
    {
        if (animSequenceController)
        {
            return true;
        }

        animSequenceController = animator.GetComponent<AnimSequenceController>();
        if (animSequenceController == null &&
            (animSequenceController = animator.GetComponentInChildren<AnimSequenceController>()) == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] {this}: Failed to find {typeof(AnimSequenceController).Name} " +
                $"on Animator object or children. If this is expected, you might want to remove this {typeof(AnimSequenceBehaviour).Name}" +
                $"from the state {stateInfo.shortNameHash}", animator);
            return false;
        }

        return true;
    }
}
