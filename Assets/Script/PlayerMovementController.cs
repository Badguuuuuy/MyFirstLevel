using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovementController : MonoBehaviour
{
    private PlayerInputController playerInput;
    private CharacterController characterController;
    private PlayerController playerController;

    [Header("Reference")]
    public GameObject playerModel;
    public Transform camPos;
    public GameObject groundCheckObj;

    [Header("Parameters")]
    public float speed = 15f;
    public float walkSpeed = 5f;
    public float slidesDampingSpeed = 30f;
    public float slidesPower = 20f;

    private Transform modelTransform;
    private GroundChecker groundChecker;
    private float slopeLimit;
    private Vector3 GroundHitNormal;
    private Vector3 m_lastVelocityXZ;

    float rotationSpeed = 3f;

    private Vector3 lastMoveDirection; // ������ �̵� ����

    Vector3 m_LastInput;
    Vector3 m_LastRawInput;

    private int jumpCnt = 0;
    private int maxJumpCnt = 2;
    private bool wasJumped = false;
    private bool m_LastIsJumping = false;


    public Action StartJump;
    public Action StartFall;
    public Action EndJump;
    public Action Turn180;
    public Action OnTurn180AnimEnd;
    public Action<int> LedgeGrap_Anim;
    public Action LedgeGrap_AnimEnd;

    [Header("Events")]
    [Tooltip("This event is sent when the player lands after a jump.")]
    public UnityEvent Landed = new();

    [Tooltip("Transition duration (in seconds) when the player changes velocity or rotation.")]
    public float Damping = 1f;

    public float moveDamping = 1f;

    [Tooltip("Ground speed when walking")]
    public float Speed = 1f;
    [Tooltip("Ground speed when sprinting")]
    public float SprintSpeed = 4;
    [Tooltip("Initial vertical speed when jumping")]
    public float JumpSpeed = 10;
    [Tooltip("Initial vertical speed when sprint-jumping")]
    public float SprintJumpSpeed = 6;

    // ���� ��ٿ� �ð�
    private float jumpCooldown = 1.2f; // 1�� ���
    private bool isJumpCooling = false;
    private float lastJumpTime = -1.3f; // ������ ���� �ð� �ʱ�ȭ
    private float _lastJumpValue;
    private float _lastCrouchValue;

    public LayerMask GroundLayers = 0;

    public enum UpModes { Player, World };

    public UpModes UpMode = UpModes.World;

    int cnt = 0;

    Vector3 UpDirection => UpMode == UpModes.World ? Vector3.up : transform.up;

    [HideInInspector] public bool IsGrounded() => GetDistanceFromGround(transform.position, UpDirection, 10) < 0.01f;
    [HideInInspector] public bool m_IsJumping { get; private set; }
    [HideInInspector] public bool m_IsSliding { get; private set; }
    [HideInInspector] public bool m_CanMove { get; set; }

    [HideInInspector] public bool m_CanTurn180 = true;

    private float Gravity = 25f;

    float m_CurrentVelocityY;
    Vector3 m_CurrentVelocityXZ;
    Vector3 m_JumpStartVelocityXZ;
    bool m_IsSprinting;


    int pakourLayerMask;

    const float kDelayBeforeInferringJump = 0.3f;
    float m_TimeLastGrounded = 0;

    private Coroutine jumpCooldownCoroutine = null;
    private Coroutine ledgeClimbCoroutine = null;

    private void OnEnable()
    {
        m_CurrentVelocityY = 0;
        m_IsSprinting = false;
        m_IsJumping = false;
        m_CanMove = true;
        m_TimeLastGrounded = Time.time;

        modelTransform = playerModel.transform;
        groundChecker = groundCheckObj.GetComponent<GroundChecker>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInputController>();
        characterController = GetComponent<CharacterController>();
        slopeLimit = characterController.slopeLimit;
        //animator = playerModel.GetComponent<Animator>();
        //StartCoroutine("Routine");
        pakourLayerMask = 1 << LayerMask.NameToLayer("ParkourStructure");
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.CurrentState != playerController.uiState)
        {
            if (playerController.CurrentState != playerController.actionState)
            {
                if (playerInput.MoveX.Value != 0f || playerInput.MoveZ.Value != 0f)
                {
                    playerController.SwitchState(playerController.moveState);
                }
                else
                {
                    playerController.SwitchState(playerController.idleState);
                }
            }
            /*
            if (m_CurrentVelocityXZ != Vector3.zero || m_CurrentVelocityY != 0f)
            {
                playerController.SwitchState(playerController.moveState);
            }
            else
            {
                playerController.SwitchState(playerController.idleState);
            }
            */
        }
    }

    private void FixedUpdate()
    {
        if (playerController.CurrentState != playerController.uiState)
        {
            LedgeGrap();
            ProcessJump();
            Move();
            Slide();
            
        }
        Debug.Log("����ī��Ʈ: " + jumpCnt);
    }
    private void Move()
    {
        if (playerController.CurrentState != playerController.actionState)
        {
            // Process Jump and gravity
            //bool justLanded = ProcessJump();

            // Get the reference frame for the input
            var rawInput = new Vector3(playerInput.MoveX.Value, 0, playerInput.MoveZ.Value);
            float cameraYRotation = camPos.transform.eulerAngles.y;
            var inputFrame = Quaternion.Euler(0, cameraYRotation, 0);
            m_LastRawInput = rawInput;

            // Read the input from the user and put it in the input frame
            m_LastInput = inputFrame * rawInput;                            /////Ȯ�� �ʿ�(ī�޶� �������� �Է� �޾� ���� ����ϴµ� ��)
            if (m_LastInput.sqrMagnitude > 1)
                m_LastInput.Normalize();

            // Compute the new velocity and move the player
            var desiredVelocity = m_LastInput * Speed;
            var damping = Damping;

            if (Vector3.Angle(m_CurrentVelocityXZ, desiredVelocity) < 100)
                m_CurrentVelocityXZ = Vector3.Slerp(
                    m_CurrentVelocityXZ, desiredVelocity,
                    Damper.Damp(1, damping, Time.deltaTime));
            //Debug.Log("����" + Damper.Damp(1, damping, Time.deltaTime));
            else
            {
                m_CurrentVelocityXZ += Damper.Damp(
                    desiredVelocity - m_CurrentVelocityXZ, damping, Time.deltaTime);
                Debug.Log("��");
            }

            m_JumpStartVelocityXZ = m_CurrentVelocityXZ;



            ApplyMotion();
        }
    }
    
    void ApplyMotion()
    {
        Debug.Log("isSliding" + m_IsSliding);
        if (characterController != null && playerController.CurrentState != playerController.actionState)
        {
            //float finalVelocityY = Mathf.Sqrt(m_CurrentVelocityY);
            Vector3 finalVelocity;
            
            if (!m_IsJumping)
            {
                if (m_IsSliding) ////������ �����̵� ��
                {
                    Vector3 slopeDir = Vector3.ProjectOnPlane(-UpDirection, GroundHitNormal).normalized;

                    m_lastVelocityXZ = Vector3.Slerp(m_lastVelocityXZ, Gravity / 2 * slopeDir, slidesDampingSpeed * Time.deltaTime);

                    finalVelocity = m_lastVelocityXZ;

                    //finalVelocity = Gravity / 2 * slopeDir;

                    Debug.Log("slopeDir: " + slopeDir + "finalVel: " + finalVelocity);
                }
                else ////������ �����̵� ���� ���� ��
                {
                    finalVelocity = Vector3.ProjectOnPlane((m_CurrentVelocityY * UpDirection + m_CurrentVelocityXZ), GroundHitNormal);
                    m_lastVelocityXZ = Vector3.ProjectOnPlane(m_CurrentVelocityXZ, GroundHitNormal);
                }
            }
            else ////���� ���� ��
            {
                finalVelocity = m_CurrentVelocityY * UpDirection + m_lastVelocityXZ;
                //m_lastVelocityXZ = Vector3.ProjectOnPlane(m_CurrentVelocityXZ, GroundHitNormal);
                m_lastVelocityXZ = Vector3.Slerp(m_lastVelocityXZ, m_CurrentVelocityXZ, Time.deltaTime);
            }

           
            //characterController.Move((m_CurrentVelocityY * UpDirection + m_CurrentVelocityXZ) * Time.deltaTime);
            characterController.Move(finalVelocity * Time.deltaTime);
        }
        /*
        else
        {
            var pos = transform.position + m_CurrentVelocityXZ * Time.deltaTime;

            // Don't fall below ground
            var up = UpDirection;
            var altitude = GetDistanceFromGround(pos, up, 10);
            if (altitude < 0 && m_CurrentVelocityY <= 0)
            {
                pos -= altitude * up;
                m_CurrentVelocityY = 0;
            }
            else if (m_CurrentVelocityY < 0)
            {
                var dy = -m_CurrentVelocityY * Time.deltaTime;
                if (dy > altitude)
                {
                    pos -= altitude * up;
                    m_CurrentVelocityY = 0;
                }
            }
            transform.position = pos + m_CurrentVelocityY * up * Time.deltaTime;
        }
        */
    }
    bool ProcessJump()
    {
        bool justLanded = false;
        var now = Time.time;
        bool grounded = IsGrounded();
        float slopeAngle = Vector3.Angle(GroundHitNormal, UpDirection);
        //bool grounded = groundChecker.isGrounded;
        //Debug.Log("�׶����: " + grounded);
        //Debug.Log("velY: " + m_CurrentVelocityY);
        bool jumpPressed = _lastJumpValue <= 0 && playerInput.Jump.Value > 0 && !isJumpCooling;

        m_CurrentVelocityY -= Gravity * Time.deltaTime;

        // Process jump command
        if (jumpPressed && jumpCnt < maxJumpCnt)
        {
            m_IsJumping = true;
            m_CurrentVelocityY = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
            //lastJumpTime = now;
            StartJump?.Invoke();
            jumpCnt++;
            wasJumped = true;
            if (jumpCooldownCoroutine == null)
            {
                jumpCooldownCoroutine = StartCoroutine("JumpCooldownCoroutine");
            }
        }
        // If we are falling, assume the jump pose
        if (!grounded && now - m_TimeLastGrounded > kDelayBeforeInferringJump && jumpCnt < maxJumpCnt)
        {
            m_IsJumping = true;
            if (!wasJumped)
                jumpCnt++;

        }
        if (m_IsJumping && !m_LastIsJumping) //������ �ԷµǾ� ����Ǳ� �����ϴ� �����ӿ����� �� �� �� ����Ǿ����.(���߿��� ��� �ݺ� ����Ǹ� �ȵ�)
        {
            StartFall?.Invoke();
            grounded = false;
        }


        if (grounded && !jumpPressed)
        {
            m_TimeLastGrounded = Time.time;
            m_CurrentVelocityY = 0;
            jumpCnt = 0;
            // If we were jumping, complete the jump
            if (m_IsJumping)
            {
                EndJump?.Invoke();
                m_IsJumping = false;
                justLanded = true;
                Landed.Invoke();
            }
        }

        m_LastIsJumping = m_IsJumping;
        _lastJumpValue = playerInput.Jump.Value;

        return justLanded;
    }
    float GetDistanceFromGround(Vector3 pos, Vector3 up, float max)
    {
        
        Vector3 boxSize = new Vector3(0.7f, 0.01f, 0.7f); // 2D ���ó�� ���
        float extraHeight = 0.15f;

        RaycastHit hit1;
        float a = 0.35f;
        //Physics.SphereCast(pos + a * up, a, -up, out hit1, 0f, GroundLayers, QueryTriggerInteraction.Ignore);

        RaycastHit hit;
        if (Physics.BoxCast(pos + up * extraHeight, boxSize / 2f, -up, out hit, Quaternion.identity, 0.5f, GroundLayers, QueryTriggerInteraction.Ignore))
        //if(Physics.SphereCast(pos + a * up, a, -up, out hit1, 0.1f, GroundLayers, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("�ٴڹڽ�ĳ��Ʈ������");
            GroundHitNormal = hit.normal;
            return hit.distance - extraHeight;
        }
        return max + 1f; // �ٴ��� ã�� ���� ���
    }

    void LedgeGrap()
    {
        if (playerController.CurrentState != playerController.actionState)
        {
            if (m_IsJumping)
            {
                bool jumpPressed = _lastJumpValue <= 0 && playerInput.Jump.Value > 0;
                if (jumpPressed)
                {
                    RaycastHit hit;
                    //RaycastHit hit1;

                    Vector3 origin = transform.position + Vector3.up * 1f; // �Ӹ� ���̿��� üũ
                    //Vector3 origin1 = transform.position + Vector3.up * 2f;

                    Vector3 forward = new Vector3(modelTransform.forward.x, transform.forward.y, modelTransform.forward.z);
                    
                    if (Physics.Raycast(origin, forward, out hit, 1f, pakourLayerMask))                    ///�𼭸��� �Ӹ� ����
                    {
                        //Vector3 ledgeCheck = hit1.point + Vector3.up * 0.5f;

                        Vector3 ledgeCheck;

                        for (float iter = 0.1f; iter <= 2f; iter += 0.1f)                       //iter == ���� ����ĳ��Ʈ�� ��� ������ ����(����������)
                        {
                            ledgeCheck = hit.point + Vector3.up * iter - forward * 0.05f;
                            if (!Physics.Raycast(ledgeCheck, forward, 1f, pakourLayerMask))
                            {
                                //Debug.Log(iter);
                                if (ledgeClimbCoroutine == null)
                                {
                                    ledgeClimbCoroutine = StartCoroutine(LedgeClimbCoroutine(ledgeCheck + forward * 0.1f, 0));// + (Vector3.up * 0.2f)));
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    IEnumerator LedgeClimbCoroutine(Vector3 endPos, int type) ///type = 0: �Ӹ�����, 1: ��������
    {

        //m_CanMove = false;

        playerController.SwitchState(playerController.actionState);

        Vector3 start = transform.position;
        //Debug.Log("����: " + transform.position);
        //Debug.Log("����: " + endPos);

        float duration = 0.25f; // 0.5f // ��ü �̵� �ð�
        if(type == 1)
        {
            duration = 0.2f;
        }

        LedgeGrap_Anim?.Invoke(type);

        float height = 0f;   // ������ ���� (���� �󸶳� �ö���)

        float elapsed = 0f;

        while (elapsed < duration)
        {
            while(playerController.CurrentState == playerController.uiState)
            {
                yield return null;
            }

            float t = elapsed / duration;

            // ���� �̵� (����~�� ��ġ ���̸� ���� ����)
            Vector3 horizontal = Vector3.Lerp(start, endPos, t);

            // ������ ������ y�� ����: 4h * t * (1 - t) ���·� � ��� �����
            float arc = 4 * height * t * (1 - t); // t(1-t) == �ε巯�� �

            transform.position = horizontal + Vector3.up * arc;

            elapsed += Time.deltaTime;
            yield return ledgeClimbCoroutine = null;
        }

        transform.position = endPos;

        TurnOn_CanMove();   

    }

    IEnumerator JumpCooldownCoroutine()
    {
        isJumpCooling = true;
        yield return new WaitForSeconds(0.2f);
        isJumpCooling = false;
        jumpCooldownCoroutine = null; //���� Ŭ������ �������̽� ������� �����ؼ� ��ٿ� ��ų���� ���� �ڷ�ƾ�� ��� �����ϱ�
    }

    void TurnOn_CanMove()
    {
        playerController.SwitchState(playerController.idleState);
        LedgeGrap_AnimEnd?.Invoke();
    }
    void Slide()
    {
        bool grounded = IsGrounded();
        bool crouchPressed = _lastCrouchValue <= 0 && playerInput.Crouch.Value > 0;

        if ((playerController.CurrentState == playerController.moveState || playerController.CurrentState == playerController.idleState) && grounded && !m_IsJumping) 
        {
            
            if (playerInput.MoveZ.Value > 0 && crouchPressed)
            {
                //Debug.Log("����");
                Vector3 pow = new Vector3(0f, 0f, slidesPower);
                Quaternion rot = Quaternion.LookRotation(m_lastVelocityXZ);
                Vector3 rotated = rot * pow;
                m_CurrentVelocityXZ += rotated;
            }
            else if (playerInput.Crouch.Value > 0)
            {
                //���� �����̵� ����
                m_IsSliding = true;

            }
            else
            {
                m_IsSliding = false;
            }
        }
        else
        {
            m_IsSliding = false;
        }
        _lastCrouchValue = playerInput.Crouch.Value;
    }
}
