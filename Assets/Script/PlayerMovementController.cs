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

    private Vector3 lastMoveDirection; // 마지막 이동 방향

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
        Debug.Log("점프카운트: " + jumpCnt);
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
            m_LastInput = inputFrame * rawInput;                            /////확인 필요(카메라 기준으로 입력 받아 벡터 계산하는듯 함)
            if (m_LastInput.sqrMagnitude > 1)
                m_LastInput.Normalize();

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
                Debug.Log("중");
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
                if (m_IsSliding) ////땅에서 슬라이딩 시
                {
                    Vector3 slopeDir = Vector3.ProjectOnPlane(-UpDirection, GroundHitNormal).normalized;

                    m_lastVelocityXZ = Vector3.Slerp(m_lastVelocityXZ, Gravity / 2 * slopeDir, slidesDampingSpeed * Time.deltaTime);

                    finalVelocity = m_lastVelocityXZ;

                    //finalVelocity = Gravity / 2 * slopeDir;

                    Debug.Log("slopeDir: " + slopeDir + "finalVel: " + finalVelocity);
                }
                else ////땅에서 슬라이딩 하지 않을 시
                {
                    finalVelocity = Vector3.ProjectOnPlane((m_CurrentVelocityY * UpDirection + m_CurrentVelocityXZ), GroundHitNormal);
                    m_lastVelocityXZ = Vector3.ProjectOnPlane(m_CurrentVelocityXZ, GroundHitNormal);
                }
            }
            else ////점프 중일 시
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
        
        Vector3 boxSize = new Vector3(0.7f, 0.01f, 0.7f); // 2D 평면처럼 사용
        float extraHeight = 0.15f;

        RaycastHit hit1;
        float a = 0.35f;
        //Physics.SphereCast(pos + a * up, a, -up, out hit1, 0f, GroundLayers, QueryTriggerInteraction.Ignore);

        RaycastHit hit;
        if (Physics.BoxCast(pos + up * extraHeight, boxSize / 2f, -up, out hit, Quaternion.identity, 0.5f, GroundLayers, QueryTriggerInteraction.Ignore))
        //if(Physics.SphereCast(pos + a * up, a, -up, out hit1, 0.1f, GroundLayers, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("바닥박스캐스트감지중");
            GroundHitNormal = hit.normal;
            return hit.distance - extraHeight;
        }
        return max + 1f; // 바닥을 찾지 못한 경우
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

        TurnOn_CanMove();   

    }

    IEnumerator JumpCooldownCoroutine()
    {
        isJumpCooling = true;
        yield return new WaitForSeconds(0.2f);
        isJumpCooling = false;
        jumpCooldownCoroutine = null; //상위 클래스나 인터페이스 등등으로 구현해서 쿨다운 스킬들을 전부 코루틴을 묶어서 관리하기
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
                //Debug.Log("앉음");
                Vector3 pow = new Vector3(0f, 0f, slidesPower);
                Quaternion rot = Quaternion.LookRotation(m_lastVelocityXZ);
                Vector3 rotated = rot * pow;
                m_CurrentVelocityXZ += rotated;
            }
            else if (playerInput.Crouch.Value > 0)
            {
                //경사면 슬라이딩 동작
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
