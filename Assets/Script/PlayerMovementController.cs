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

    // 커스텀 보정 범위 설정
    float slopeMinBoost = 0.5f;   // 경사면 역방향일 때 최소 속도
    float slopeMaxBoost = 2f;   // 경사면 방향일 때 최대 속도

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

    private Vector3 lastMoveDirection; // 마지막 이동 방향

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

    // 점프 쿨다운 시간
    private float jumpCooldown = 1.2f; // 1초 대기
    private bool isJumpCooling = false;
    private float lastJumpTime = -1.3f; // 마지막 점프 시각 초기화
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
            if (projected.sqrMagnitude < 1e-6f)  // 너무 작으면
            {
                slopeDir = UpDirection; // 또는 적절한 기본값
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
            //Debug.Log("점프카운트: " + jumpCnt);
            //Debug.Log("슬로프 방향 대비 좌우입력값: " + x);
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
        m_LastInput = inputFrame * rawInput;                            /////확인 필요(카메라 기준으로 입력 받아 벡터 계산하는듯 함)
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
            m_LastInput = inputFrame * rawInput;                            /////확인 필요(카메라 기준으로 입력 받아 벡터 계산하는듯 함)
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
            //Debug.Log("댐핑" + Damper.Damp(1, damping, Time.deltaTime));
            else
            {
                m_CurrentVelocityXZ += Damper.Damp(
                    desiredVelocity - m_CurrentVelocityXZ, damping, Time.deltaTime);
                //Debug.Log("중");
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
                if (m_IsSliding) ////땅에서 슬라이딩 시
                {
                    Vector3 slopeRight;

                    ////슬로프 기준 좌우 방향
                    if (Vector3.Angle(GroundHitNormal, Vector3.up) == 0f) // 거의 평지일 때
                    {
                        slopeRight = camPos.transform.right; // 카메라 기준 오른쪽 벡터를 사용
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
                            Debug.Log("경사슬라이딩중");
                            m_SlideVelocityXZ = Vector3.Lerp(m_SlideVelocityXZ, (Gravity / 2f) * slopeDir + x * 20f, slidesDampingSpeed * Time.deltaTime); //기존 슬라이딩
                        }
                        else
                        {
                            m_SlideVelocityXZ = Vector3.Lerp(m_SlideVelocityXZ, Vector3.zero, slidesDampingSpeed * Time.deltaTime);
                            //m_SlideVelocityXZ = Vector3.Lerp(m_SlideVelocityXZ, m_SlideVelocityXZ.normalized + x * 20f, slidesDampingSpeed * Time.deltaTime);
                        }
                    }
                    
                    finalVelocity = m_SlideVelocityXZ;
                }
                else if (m_IsCrouching) ////땅에서 앉아서 이동할 시
                {
                    finalVelocity = Vector3.ProjectOnPlane(((m_CurrentVelocityXZ / 2)), GroundHitNormal) + (m_CurrentVelocityY * UpDirection);
                    m_SlideVelocityXZ = Vector3.ProjectOnPlane(m_CurrentVelocityXZ, GroundHitNormal);
                }
                else ////땅에서 서서 이동할 시
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
            else ////점프 중일 시
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
        //Debug.Log("그라운디드: " + grounded);
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
        if (m_IsJumping && !m_LastIsJumping) //점프가 입력되어 실행되기 시작하는 프레임에서만 딱 한 번 실행되어야함.(공중에서 계속 반복 실행되면 안됨)
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
        
        Vector3 boxSize = new Vector3(0.85f, 0.01f, 0.85f); // 2D 평면처럼 사용
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
                // 이상하면 기본값 할당
                Debug.Log("NAN!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                GroundHitNormal = Vector3.up;
            }
            else
            {
                //Debug.Log("바닥박스캐스트감지중");
                GroundHitNormal = hit.normal;
                return hit.distance - extraHeight;
                
            }
            
        }
        else
        {
            GroundHitNormal = Vector3.up;
        }
        return max + 1f; // 바닥을 찾지 못한 경우
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

                    Vector3 origin = transform.position + Vector3.up * 1f; // 머리 높이에서 체크
                    //Vector3 origin1 = transform.position + Vector3.up * 2f;

                    Vector3 forward = new Vector3(modelTransform.forward.x, transform.forward.y, modelTransform.forward.z);
                    
                    if (Physics.Raycast(origin, forward, out hit, 1f, pakourLayerMask))                    ///모서리가 머리 높이
                    {
                        //Vector3 ledgeCheck = hit1.point + Vector3.up * 0.5f;

                        Vector3 ledgeCheck;

                        for (float iter = 0.1f; iter <= 2f; iter += 0.1f)                       //iter == 위로 레이캐스트를 쏘는 높이의 간격(촘촘한정도)
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

    IEnumerator LedgeClimbCoroutine(Vector3 endPos, int type) ///type = 0: 머리높이, 1: 가슴높이
    {

        //m_CanMove = false;

        playerController.SwitchState(playerController.actionState);

        Vector3 start = transform.position;
        //Debug.Log("시작: " + transform.position);
        //Debug.Log("종료: " + endPos);

        float duration = 0.25f; // 0.5f // 전체 이동 시간
        if(type == 1)
        {
            duration = 0.2f;
        }

        LedgeGrap_Anim?.Invoke(type);

        float height = 0f;   // 포물선 높이 (위로 얼마나 올라갈지)

        float elapsed = 0f;

        while (elapsed < duration)
        {
            while(playerController.CurrentState == playerController.uiState)
            {
                yield return null;
            }

            float t = elapsed / duration;

            // 수평 이동 (시작~끝 위치 사이를 선형 보간)
            Vector3 horizontal = Vector3.Lerp(start, endPos, t);

            // 포물선 형태의 y값 보간: 4h * t * (1 - t) 형태로 곡선 모양 만들기
            float arc = 4 * height * t * (1 - t); // t(1-t) == 부드러운 곡선

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
        jumpCooldownCoroutine = null; //상위 클래스나 인터페이스 등등으로 구현해서 쿨다운 스킬들을 전부 코루틴을 묶어서 관리하기
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
                    if (Vector3.Angle(GroundHitNormal, Vector3.up) == 0f) // 거의 평지일 때
                    {
                        m_IsInSlope = false;
                        //Debug.Log(m_SlideVelocityXZ);
                        //Debug.Log(m_SlideVelocityXZ.magnitude);
                        if (m_SlideVelocityXZ.magnitude < 1f)
                        {
                            m_IsSliding = false;
                        }
                    }
                    else if (Vector3.Dot(m_SlideVelocityXZ, slopeDir) < 1f) // 경사면일 때
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
                Debug.Log("야");
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