using NobunAtelier;
using UnityEngine;

public class AnimSequenceBehaviour : StateMachineBehaviour
{
    [SerializeField] private AnimSegmentDefinition m_animSegmentEnterDefinition;
    [SerializeField] private AnimSegmentDefinition m_animSegmentExitDefinition;
    private AnimSequenceController animSequenceController;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!m_animSegmentEnterDefinition || !RefreshAnimSequenceController(animator, stateInfo))
        {
            return;
        }

        animSequenceController.OnAnimationSegmentTrigger(m_animSegmentEnterDefinition);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!m_animSegmentExitDefinition || !RefreshAnimSequenceController(animator, stateInfo))
        {
            return;
        }

        animSequenceController.OnAnimationSegmentTrigger(m_animSegmentExitDefinition);
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
