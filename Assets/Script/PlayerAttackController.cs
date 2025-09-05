using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections;
using UnityEngine.Events;

public class PlayerAttackController : MonoBehaviour
{
    public GameObject playerModel;
    PlayerController playerController;
    PlayerInputController playerInputController;

    PlayerAnimationController playerAnimationController;
    PlayerMovementController playerMovementController;

    public UnityEvent OnAttackComboMove;
    public UnityEvent OnAttackCombo;

    public bool canCombo = true;

    private void Awake()
    {
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerAnimationController = playerModel.GetComponent<PlayerAnimationController>();
        playerMovementController = GetComponent<PlayerMovementController>();
        playerController = GetComponent<PlayerController>();
        playerInputController = GetComponent<PlayerInputController>();

        OnAttackComboMove.AddListener(() => StartCoroutine(Attack1MoveCoroutine()));
        OnAttackCombo.AddListener(() => StartCoroutine(Attack1Coroutine()));
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInputController.Attack.Value >= 1f)
        {
            if(!(playerMovementController.m_fsm.currentState is PlayerMovementController.ClimbState) && canCombo)
                playerAnimationController.PlayAnim_Attack1();
        }
    }
    public void UseAttack1()
    {
        if (!(playerMovementController.m_fsm.currentState is PlayerMovementController.ClimbState) && canCombo)
        {
            playerAnimationController.PlayAnim_Attack1();
            //StartCoroutine(Attack1Coroutine());
        }
    }

    IEnumerator Attack1MoveCoroutine()
    {
        playerMovementController.m_CanMove = false;

        // DOTween을 사용하여 돌진
        yield return transform.DOMove(transform.position + transform.forward * 4f, 0.5f)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion(); // 이동이 끝날 때까지 기다림

        playerMovementController.m_CanMove = true;
    }
    IEnumerator Attack1Coroutine()
    {
        playerMovementController.m_CanMove = false;

        Debug.Log(playerMovementController.m_CanMove);

        yield return new WaitForSeconds(0.5f);

        playerMovementController.m_CanMove = true;
    }
}
