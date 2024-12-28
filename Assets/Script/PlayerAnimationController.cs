using System;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    Animator animator;               // 애니메이터
    CharacterController charController; // CharacterController
    PlayerController playerController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();

        playerController.OnRightClickedToAim += ToAimCamera;
        playerController.OnRightClickedToFreeLook += ToFreeLookCamera;
    }

    // Update is called once per frame
    void Update()
    {
        if ((playerController.verticalInputRaw != 0f) || (playerController.horizontalInputRaw != 0f)) //////wasd 입력이 하나라도 있으면
        {
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }

        animator.SetFloat("Pos X", playerController.horizontalInput);
        animator.SetFloat("Pos Y", playerController.verticalInput);
    }

    private void ToAimCamera(object sender, EventArgs e)
    {
        animator.SetBool("Aiming", true);
    }
    private void ToFreeLookCamera(object sender, EventArgs e)
    {
        animator.SetBool("Aiming", false);
    }
}
