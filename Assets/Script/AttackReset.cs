using UnityEngine;
using System.Collections;

public class AttackReset : StateMachineBehaviour
{
    public enum AttackType
    {
        Attack1,
        Attack2
    }

    [SerializeField] string triggerName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        /*
        if (stateInfo.IsName("Attack_Combo1") || stateInfo.IsName("Attack_Combo2") || stateInfo.IsName("Attack_Combo3"))
        {
            var attackController = animator.GetComponent<PlayerAttackController>();

            if (stateInfo.IsName("Attack_Combo1") || stateInfo.IsName("Attack_Combo3"))
            {
                if (attackController != null)
                {
                    attackController.OnAttackComboMove?.Invoke();
                }
            }
            else
            {
                if (attackController != null)
                {
                    attackController.OnAttackCombo?.Invoke();
                }
            }
        }
        */

        animator.SetBool("RotSpine", true);
        animator.SetBool("Equiping", true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger(triggerName);   
    }

    //OnStateExit, OnStateEnter 등을 이용해 공격 타이밍을 캐치하고, stateinfo를 확인한 후에 어떤 공격 기술인지 확인해서 attackController로 넘겨줌
}
