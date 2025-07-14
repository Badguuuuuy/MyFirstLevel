using UnityEngine;

public static class AnimatorExtensions
{
    public static T FindComponentInRoot<T>(this Animator animator) where T : Component
    {
        return animator.transform.root.GetComponent<T>();
    }

    ///나중에 확장 메서드를 더 만들일이 있으면 꼭 다른 cs파일로 분리해서 모아두기!!!!!!!!!!!!!
}

public class ResetToDefault : StateMachineBehaviour
{
    PlayerAttackController attackController;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackController = animator.FindComponentInRoot<PlayerAttackController>();

        attackController.canCombo = true;

        animator.SetBool("RotSpine", false);
        animator.SetBool("Equiping", false);
        PlayerIdleAnim_Inter.Unequiping();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackController = animator.FindComponentInRoot<PlayerAttackController>();

        attackController.canCombo = false;
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
    //}
}
