using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    private Animator animator; 
    private PlayerController playerController;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector3 moveDirectionRaw;

    private float moveSpeed;
    public float speed = 10f;

    public float rotationSpeed = 10f;
    private Vector3 move;
    private Quaternion targetRotation;

    private bool isAiming = false;
    private bool isAimingToggle = false;

    public CinemachineCamera mainCam;
    public CinemachineCamera aimCam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        //StartCoroutine("Routine");
    }

    // Update is called once per frame
    void Update()
    {
        isAiming = playerController.rightClickInput;
        if (isAiming)
        {
            isAimingToggle = !isAimingToggle;
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        //if (isAiming)
        if (isAimingToggle)
        {
            moveDirection = (transform.forward * playerController.verticalInput + transform.right * playerController.horizontalInput).normalized;
            moveSpeed = Mathf.Min((transform.forward * playerController.verticalInput + transform.right * playerController.horizontalInput).magnitude, 1.0f) * speed;
            characterController.SimpleMove(moveDirection * moveSpeed);
        }
        else
        {
            //////플레이어 이동 계산
            moveDirection = (mainCam.transform.forward * playerController.verticalInput + mainCam.transform.right * playerController.horizontalInput).normalized;
            moveSpeed = Mathf.Min((mainCam.transform.forward * playerController.verticalInput + mainCam.transform.right * playerController.horizontalInput).magnitude, 1.0f) * speed;

            //////플레이어 이동
            characterController.SimpleMove(moveDirection * moveSpeed);

            moveDirectionRaw = (mainCam.transform.forward * playerController.verticalInputRaw + mainCam.transform.right * playerController.horizontalInputRaw).normalized;

            //////Freelook 플레이어 로테이션
            if (moveDirectionRaw != Vector3.zero)
            {
                targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(moveDirection).eulerAngles.y, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = transform.rotation;
            }
        }
    }
}
