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
    private Rigidbody rb;

    [Header("Reference")]
    public GameObject playerModel;
    public Transform camPos;
    public GameObject groundCheckObj;

    [Header("Parameters")]
    public float speed = 15f;
    public float walkSpeed = 5f;
    public float slidesDampingSpeed = 30f;
    public float slidesPower = 1.5f;

    private bool grounded;
    private Vector3 deltaPosition;

    // Ŀ���� ���� ���� ����
    float slopeMinBoost = 0.5f;   // ���� �������� �� �ּ� �ӵ�
    float slopeMaxBoost = 2f;   // ���� ������ �� �ִ� �ӵ�

    private Transform modelTransform;
    //private GroundChecker groundChecker;
    private float slopeLimit;
    private Vector3 GroundHitNormal;
    private Vector3 m_SlideVelocityXZ;
    private Vector3 m_SlideStartVelocityXZ;
    private float slidesAccel_Elapsed = 0f;
    Vector3 slopeDir;
    private bool m_IsInSlope = false;

    private float slideFriction = 10f;

    float rotationSpeed = 3f;

    private Vector3 lastMoveDirection; // ������ �̵� ����

    Vector3 m_LastInput;
    Vector3 m_LastRawInput;

    private int jumpCnt = 0;
    private int maxJumpCnt = 2;
    private bool wasJumped = false;
    private bool m_LastIsJumping = false;
    private bool m_wasCrouching = false;
    private bool crouchingPressed = false;


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

    //[Tooltip("Ground speed when walking")]
    //public float Speed = 1f;
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

    //[HideInInspector] public bool IsGrounded() => GetDistanceFromGround(transform.position, UpDirection, 10) < 0.01f;
    [HideInInspector] public bool m_IsJumping { get; private set; }
    [HideInInspector] public bool m_IsCrouching { get; private set; }
    [HideInInspector] public bool m_IsSliding { get; private set; }

    [HideInInspector] public bool m_IsAccelSliding { get; private set; }
    [HideInInspector] public bool m_CanMove { get; set; }

    [HideInInspector] public bool m_CanTurn180 = true;

    private float Gravity = 25f;

    float m_CurrentVelocityY;
    Vector3 m_CurrentVelocityXZ;
    Vector3 m_JumpStartVelocityXZ;
    bool m_IsSprinting;

    Vector3 x;


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
        //groundChecker = groundCheckObj.GetComponent<GroundChecker>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInputController>();
        //characterController = GetComponent<CharacterController>();
        //slopeLimit = characterController.slopeLimit;
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
            GroundCheck(rb.position, UpDirection);
            Move();
        }
            /*
            grounded = IsGrounded();
            //slopeDir = Vector3.ProjectOnPlane(-UpDirection, GroundHitNormal).normalized;
            Vector3 projected = Vector3.ProjectOnPlane(-UpDirection, GroundHitNormal);
            if (projected.sqrMagnitude < 1e-6f)  // �ʹ� ������
            {
                slopeDir = UpDirection; // �Ǵ� ������ �⺻��
            }
            else
            {
                slopeDir = projected.normalized;
            }
            if (playerController.CurrentState != playerController.uiState)
            {
                LedgeGrap();
                ProcessJump();
                Move();
                Slide();

            }
            //Debug.Log("Grounded: " + grounded);
            //Debug.Log("����ī��Ʈ: " + jumpCnt);
            //Debug.Log("������ ���� ��� �¿��Է°�: " + x);
            */
    }
    private void Move()
    {
        // Get the reference frame for the input
        var rawInput = new Vector3(playerInput.MoveX.Value, 0, playerInput.MoveZ.Value);
        float cameraYRotation = camPos.transform.eulerAngles.y;
        var inputFrame = Quaternion.Euler(0, cameraYRotation, 0);
        m_LastRawInput = rawInput;

        // Read the input from the user and put it in the input frame
        m_LastInput = inputFrame * rawInput;                            /////Ȯ�� �ʿ�(ī�޶� �������� �Է� �޾� ���� ����ϴµ� ��)
        if (m_LastInput.sqrMagnitude > 1)
            m_LastInput.Normalize();

        deltaPosition = m_LastInput * speed * Time.fixedDeltaTime;

        var slopePosition = Vector3.ProjectOnPlane(deltaPosition, GroundHitNormal).normalized * deltaPosition.magnitude;

        var slopeAngle = Vector3.ProjectOnPlane(-UpDirection, GroundHitNormal);

        float alignment = Vector3.Dot(slopePosition.normalized, slopeAngle.normalized);

        float t = (alignment + 1f) * 0.5f;
        float slopeBoost = Mathf.Lerp(slopeMinBoost, slopeMaxBoost, t);

        rb.MovePosition(rb.position + (slopePosition * slopeBoost));

        //Debug.Log("linearVelocity: " + rb.linearVelocity.magnitude);
        //Debug.Log("GroundHitNormal: " + GroundHitNormal);
        Debug.Log("deltaPosition.magnitude: " + deltaPosition.magnitude + "slopePosition.magnitude: " + slopePosition.magnitude);
    }
    /*
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

            //Debug.Log("m_LastInput: " + m_LastInput);

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
                //Debug.Log("��");
            }

            m_JumpStartVelocityXZ = m_CurrentVelocityXZ;



            ApplyMotion();
        }
    }*/
    /*
    void ApplyMotion()
    {
        
        if (characterController != null && playerController.CurrentState != playerController.actionState)
        {
            //float finalVelocityY = Mathf.Sqrt(m_CurrentVelocityY);
            Vector3 finalVelocity;
            
            if (!m_IsJumping)
            {
                if (m_IsSliding) ////������ �����̵� ��
                {
                    Vector3 slopeRight;

                    ////������ ���� �¿� ����
                    if (Vector3.Angle(GroundHitNormal, Vector3.up) == 0f) // ���� ������ ��
                    {
                        slopeRight = camPos.transform.right; // ī�޶� ���� ������ ���͸� ���
                    }
                    else
                    {
                        slopeRight = Vector3.Cross(GroundHitNormal, slopeDir).normalized;
                    }
                    
                    
                    var rawInput = new Vector3(playerInput.MoveX.Value, 0f, 0f);
                    float cameraYRotation = camPos.transform.eulerAngles.y;
                    var inputFrame = Quaternion.Euler(0, cameraYRotation, 0);
                    var lastXInput = inputFrame * rawInput;
                    if (lastXInput.sqrMagnitude > 1)
                        lastXInput.Normalize();

                    Vector3 projected = Vector3.Project(lastXInput, slopeRight);
                    x = projected;

                    if (m_IsAccelSliding)
                    {

                        slidesAccel_Elapsed += Time.deltaTime;
                        if (slidesAccel_Elapsed < 0.2f)
                        {
                            if (Vector3.Angle(GroundHitNormal, Vector3.up) == 0f) 
                            {
                                m_SlideVelocityXZ = m_SlideStartVelocityXZ * slidesPower;
                            }
                            else
                            {
                                m_SlideVelocityXZ = Vector3.ProjectOnPlane(m_SlideStartVelocityXZ * slidesPower, GroundHitNormal);
                            }
                        }
                        else
                        {
                            m_IsAccelSliding = false;
                            slidesAccel_Elapsed = 0f;
                        }
                    }
                    else
                    {
                        if (m_IsInSlope)
                        {
                            Debug.Log("��罽���̵���");
                            m_SlideVelocityXZ = Vector3.Lerp(m_SlideVelocityXZ, (Gravity / 2f) * slopeDir + x * 20f, slidesDampingSpeed * Time.deltaTime); //���� �����̵�
                        }
                        else
                        {
                            m_SlideVelocityXZ = Vector3.Lerp(m_SlideVelocityXZ, Vector3.zero, slidesDampingSpeed * Time.deltaTime);
                            //m_SlideVelocityXZ = Vector3.Lerp(m_SlideVelocityXZ, m_SlideVelocityXZ.normalized + x * 20f, slidesDampingSpeed * Time.deltaTime);
                        }
                    }
                    
                    finalVelocity = m_SlideVelocityXZ;
                }
                else if (m_IsCrouching) ////������ �ɾƼ� �̵��� ��
                {
                    finalVelocity = Vector3.ProjectOnPlane(((m_CurrentVelocityXZ / 2)), GroundHitNormal) + (m_CurrentVelocityY * UpDirection);
                    m_SlideVelocityXZ = Vector3.ProjectOnPlane(m_CurrentVelocityXZ, GroundHitNormal);
                }
                else ////������ ���� �̵��� ��
                {
                    finalVelocity = Vector3.ProjectOnPlane(((m_CurrentVelocityXZ)), GroundHitNormal) + (m_CurrentVelocityY * UpDirection);
                    m_SlideVelocityXZ = Vector3.ProjectOnPlane(m_CurrentVelocityXZ, GroundHitNormal);
                }
            }
            else if (crouchingPressed)
            {
                Debug.Log("12");
                finalVelocity = m_CurrentVelocityY * UpDirection + m_SlideVelocityXZ;
            }
            else ////���� ���� ��
            {
                //finalVelocity = m_CurrentVelocityY * UpDirection + m_SlideVelocityXZ;
                finalVelocity = m_CurrentVelocityY * UpDirection + m_CurrentVelocityXZ;
                m_SlideVelocityXZ = m_CurrentVelocityXZ;

                //m_SlideVelocityXZ = Vector3.Slerp(m_SlideVelocityXZ, m_CurrentVelocityXZ, Time.deltaTime);
            }


            //characterController.Move((m_CurrentVelocityY * UpDirection + m_CurrentVelocityXZ) * Time.deltaTime);
            //Debug.Log("finalVelocity: " + finalVelocity + "m_SlideVelocityXZ: " + m_SlideVelocityXZ + "m_CurrentVelocityXZ: " + m_CurrentVelocityXZ + "GroundHitNormal: " + GroundHitNormal + "slopeDir: " + slopeDir);
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
        
    }*/
    /*
    bool ProcessJump()
    {
        bool justLanded = false;
        var now = Time.time;
        
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
            //grounded = false;
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
    */

    private void GroundCheck(Vector3 pos, Vector3 up)
    {
        RaycastHit hit;
        if (Physics.Raycast(pos, -up, out hit, 0.1f, GroundLayers, QueryTriggerInteraction.Ignore))
        {
            GroundHitNormal = hit.normal;
            grounded = true;
        }
        else
        {
            GroundHitNormal = up;
            grounded = false;
        }
    }

    /*
    float GetDistanceFromGround(Vector3 pos, Vector3 up, float max)
    {
        
        Vector3 boxSize = new Vector3(0.85f, 0.01f, 0.85f); // 2D ���ó�� ���
        float extraHeight = 0.15f;
        float extra = 0.1f;
        //RaycastHit hit1;
        float a = 0.35f;
        //Physics.SphereCast(pos + a * up, a, -up, out hit1, 0f, GroundLayers, QueryTriggerInteraction.Ignore);

        Physics.Raycast(pos, Vector3.forward, 1f, GroundLayers, QueryTriggerInteraction.Ignore);

        RaycastHit hit;
        if (Physics.BoxCast(pos + up * extraHeight, boxSize / 2f, -up, out hit, Quaternion.identity, 0.5f, GroundLayers, QueryTriggerInteraction.Ignore))
        //if(Physics.SphereCast(pos + a * up, a, -up, out hit1, 0.1f, GroundLayers, QueryTriggerInteraction.Ignore))
        //if(Physics.Raycast(pos - up * extra, dir, out hit, 1f, GroundLayers, QueryTriggerInteraction.Ignore))
        {
            if (float.IsNaN(hit.normal.x) || float.IsNaN(hit.normal.y) || float.IsNaN(hit.normal.z))
            {
                // �̻��ϸ� �⺻�� �Ҵ�
                Debug.Log("NAN!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                GroundHitNormal = Vector3.up;
            }
            else
            {
                //Debug.Log("�ٴڹڽ�ĳ��Ʈ������");
                GroundHitNormal = hit.normal;
                return hit.distance - extraHeight;
                
            }
            
        }
        else
        {
            GroundHitNormal = Vector3.up;
        }
        return max + 1f; // �ٴ��� ã�� ���� ���
    }
    */

    /*
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

        playerController.SwitchState(playerController.idleState);
        LedgeGrap_AnimEnd?.Invoke();

    }
    */
    /*
    IEnumerator JumpCooldownCoroutine()
    {
        isJumpCooling = true;
        yield return new WaitForSeconds(0.2f);
        isJumpCooling = false;
        jumpCooldownCoroutine = null; //���� Ŭ������ �������̽� ������� �����ؼ� ��ٿ� ��ų���� ���� �ڷ�ƾ�� ��� �����ϱ�
    }
    */
    /*
    void Slide()
    {
        if ((playerController.CurrentState == playerController.moveState || playerController.CurrentState == playerController.idleState)) 
        {
            if (playerInput.Crouch.Value > 0 && grounded && !m_IsJumping)
            {
                m_IsCrouching = true;

                bool crouchingStarted = m_IsCrouching && !m_wasCrouching;

                if (playerInput.MoveZ.Value > 0 && crouchingStarted)
                {
                    m_IsSliding = true;
                    m_IsAccelSliding = true;
                    m_SlideStartVelocityXZ = m_CurrentVelocityXZ;
                    
                }
                //if (m_SlideVelocityXZ.magnitude < 3f)
                if (!m_IsAccelSliding)
                {
                    if (Vector3.Angle(GroundHitNormal, Vector3.up) == 0f) // ���� ������ ��
                    {
                        m_IsInSlope = false;
                        //Debug.Log(m_SlideVelocityXZ);
                        //Debug.Log(m_SlideVelocityXZ.magnitude);
                        if (m_SlideVelocityXZ.magnitude < 1f)
                        {
                            m_IsSliding = false;
                        }
                    }
                    else if (Vector3.Dot(m_SlideVelocityXZ, slopeDir) < 1f) // ������ ��
                    {
                        m_IsInSlope = true;
                        m_IsSliding = false;
                    }
                }
            }
            else
            {
                m_IsCrouching = false;
                m_IsSliding = false;
            }
            if (playerInput.Crouch.Value > 0)
            {
                Debug.Log("��");
                crouchingPressed = true;
            }
            else
            {
                crouchingPressed = false;
            }
        }
        m_wasCrouching = m_IsCrouching;
        _lastCrouchValue = playerInput.Crouch.Value;
    }
    bool IsWithinBounds(Vector3 pos, float maxValue)
    {
        return Mathf.Abs(pos.x) <= maxValue && Mathf.Abs(pos.z) <= maxValue;
    }*/
}