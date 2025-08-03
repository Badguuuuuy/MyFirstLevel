using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using UnityEngine.Windows;
using static UnityEngine.Rendering.DebugUI.Table;

public class PlayerMovementController : MonoBehaviour
{
    public abstract class State
    {
        protected StateMachine fsm;
        protected PlayerMovementController m_player;

        public State(StateMachine fsm, PlayerMovementController m_player) { this.fsm = fsm; this.m_player = m_player; }

        public abstract void Enter();
        public abstract void FixedUpdate();
        public abstract void HandleInput();
        public abstract void Exit();
    }

    // -----------------------
    // 상태머신 클래스
    // -----------------------
    public class StateMachine
    {
        public State currentState { get; private set; }

        public void ChangeState(State newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }

        public void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }
        public void HandleInput()
        {
            currentState?.HandleInput();
        }
    }

    // -----------------------
    // 예시 상태 (Move)
    // -----------------------
    public class MoveState : State
    {
        
        public MoveState(StateMachine fsm, PlayerMovementController m_player) : base(fsm, m_player) 
        {
            
        }

        public override void Enter() { /* 상태 진입 시 실행 */ }
        public override void FixedUpdate() 
        { /* 매 프레임 검사 */
            if (m_player.dodgePressed)
            {
                fsm.ChangeState(new DashState(fsm, m_player));
                return;
            }
            else if (!m_player.grounded && Time.fixedTime - m_player.m_TimeLastGrounded > kDelayBeforeInferringJump)
            {
                fsm.ChangeState(new FallState(fsm, m_player));
                return;
            }
            else if (m_player.jumpPressed && m_player.grounded) //&& m_player.jumpCnt < m_player.maxJumpCnt)
            {
                fsm.ChangeState(new JumpState(fsm, m_player, 0));
                return;
            }
            else if (m_player.playerInput.Crouch.Value > 0f && m_player.playerInput.MoveZ.Value > 0f)
            {
                fsm.ChangeState(new AccelSlideState(fsm, m_player));
                return;
            }
            else if (m_player.playerInput.Crouch.Value > 0f)
            {
                fsm.ChangeState(new CrouchState(fsm, m_player));
                return;
            }
            m_player.Move(fsm, m_player);
            
            //fsm.ChangeState(new JumpState(fsm, m_player));
        }
        public override void HandleInput() { }
        public override void Exit() { /* 상태 종료 시 실행 */ }
    }
    // -----------------------
    // 예시 상태 (Jump)
    // -----------------------
    public class JumpState : State
    {
        //jumpType
        //0: flat jump
        //1: wall jump
        private int _jumpType;
        public JumpState(StateMachine fsm, PlayerMovementController m_player, int jumpType) : base(fsm, m_player)
        {
            _jumpType = jumpType;
        }
        
        public override void Enter() 
        { /* 상태 진입 시 실행 */
            m_player.StartJump_Anim?.Invoke();
            m_player.Jump(fsm, m_player, _jumpType);
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */
            if (m_player.dodgePressed)
            {
                fsm.ChangeState(new DashState(fsm, m_player));
                return;
            }
            else if (m_player.jumpPressed)
            {
                m_player.LedgeGrapCheck(fsm, m_player);
                return;
            }
            else if(m_player.wallDetected && m_player.playerInput.MoveZ.Value >= 1f && m_player.speedXZ > 1f)
            {
                fsm.ChangeState(new WallRunState(fsm, m_player));
                return;
            }
            else if (!m_player.grounded && m_player.speedY < 0f)
            {
                fsm.ChangeState(new FallState(fsm, m_player));
                return;
            }
            else if (m_player.grounded)
            {
                fsm.ChangeState(new MoveState(fsm, m_player));
                return;
            }
            /*
            m_player.rb.AddForce(m_player.speed * m_player.m_LastInput, ForceMode.Acceleration);

            Vector3 v = m_player.rb.linearVelocity;

            // XZ축만 감쇠 적용 (y축은 그대로 둠)
            v.x *= 1f / (1f + m_player.midAirDampingXZ * Time.fixedDeltaTime);
            v.z *= 1f / (1f + m_player.midAirDampingXZ * Time.fixedDeltaTime);

            m_player.rb.linearVelocity = v;
            */

        }
        public override void HandleInput() { }
        public override void Exit() { /* 상태 종료 시 실행 */ }
    }
    // -----------------------
    // 예시 상태 (Fall)
    // -----------------------
    public class FallState : State
    {

        public FallState(StateMachine fsm, PlayerMovementController m_player) : base(fsm, m_player)
        {

        }

        public override void Enter() 
        { /* 상태 진입 시 실행 */
            m_player.StartFall_Anim?.Invoke();
            
            if (!m_player.wasJumped)
                m_player.jumpCnt++;
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */
            if (m_player.dodgePressed)
            {
                fsm.ChangeState(new DashState(fsm, m_player));
                return;
            }
            else if (m_player.jumpPressed)
            {
                m_player.LedgeGrapCheck(fsm, m_player);
                return;
            }
            else if (m_player.wallDetected && m_player.playerInput.MoveZ.Value >= 1f && m_player.speedXZ > 1f)
            {
                fsm.ChangeState(new WallRunState(fsm, m_player));
                return;
            }
            else if(m_player.grounded && m_player.playerInput.Crouch.Value > 0f && m_player.playerInput.MoveZ.Value > 0f)
            {
                fsm.ChangeState(new AccelSlideState(fsm, m_player));
                return;
            }
            else if (m_player.grounded)
            {
                fsm.ChangeState(new MoveState(fsm, m_player));
                return;
            }
            
            m_player.Fall(fsm, m_player);
            /*
            Vector3 v = m_player.rb.linearVelocity;

            // XZ축만 감쇠 적용 (y축은 그대로 둠)
            v.x *= 1f / (1f + m_player.midAirDampingXZ * Time.fixedDeltaTime);
            v.z *= 1f / (1f + m_player.midAirDampingXZ * Time.fixedDeltaTime);

            m_player.rb.linearVelocity = v;
            */
        }
        public override void HandleInput() { }
        public override void Exit() 
        { /* 상태 종료 시 실행 */
            if (m_player.grounded)
            {
                m_player.jumpCnt = 0;
            }
            m_player.EndJump_Anim?.Invoke();
        }
    }
    // -----------------------
    // 예시 상태 (Crouch)
    // -----------------------
    public class CrouchState : State
    {
        
        public CrouchState(StateMachine fsm, PlayerMovementController m_player) : base(fsm, m_player)
        {
            
        }

        public override void Enter() 
        { /* 상태 진입 시 실행 */ 
            m_player.StartCrouch_Anim?.Invoke();
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */
            if (m_player.dodgePressed)
            {
                fsm.ChangeState(new DashState(fsm, m_player));
                return;
            }
            else if (!m_player.grounded && Time.fixedTime - m_player.m_TimeLastGrounded > kDelayBeforeInferringJump)
            {
                fsm.ChangeState(new FallState(fsm, m_player));
                return;
            }
            else if (m_player.jumpPressed && m_player.grounded)// && m_player.jumpCnt < m_player.maxJumpCnt)
            {
                fsm.ChangeState(new JumpState(fsm, m_player, 0));
                return;
            }
            else if (m_player.playerInput.Crouch.Value <= 0f)
            {
                fsm.ChangeState(new MoveState(fsm, m_player));
                return;
            }
            m_player.Crouch();
        }
        public override void HandleInput() { }
        public override void Exit() 
        { /* 상태 종료 시 실행 */
            m_player.EndCrouch_Anim?.Invoke();
        }
    }
    // -----------------------
    // 예시 상태 (Slide)
    // -----------------------
    public class SlideState : State
    {
        Vector3 slideDir;
        public SlideState(StateMachine fsm, PlayerMovementController m_player, Vector3 slideStartDir) : base(fsm, m_player)
        {
            slideDir = slideStartDir;
        }

        public override void Enter() 
        { /* 상태 진입 시 실행 */
            m_player.StartSlide_Anim?.Invoke();
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */
            if (m_player.dodgePressed)
            {
                fsm.ChangeState(new DashState(fsm, m_player));
                return;
            }
            else if (!m_player.grounded && Time.fixedTime - m_player.m_TimeLastGrounded > kDelayBeforeInferringJump)
            {
                fsm.ChangeState(new FallState(fsm, m_player));
                return;
            }
            else if (m_player.jumpPressed && m_player.grounded)// && m_player.jumpCnt < m_player.maxJumpCnt)
            {
                fsm.ChangeState(new JumpState(fsm, m_player, 0));
                return;
            }
            else if (m_player.crouchingReleased)
            {
                fsm.ChangeState(new MoveState(fsm, m_player));
                return;
            }
            else if (((Mathf.Abs(m_player.speedY) < 0.1f && m_player.speedXZ < 0.25f)) || Vector3.Angle(m_player.cameraYdir, m_player.horizontalVelocity) > 130f)
            {
                fsm.ChangeState(new CrouchState(fsm, m_player));
                return;
            }
            m_player.Slide(fsm, m_player, slideDir);
        }
        public override void HandleInput() { }
        public override void Exit() 
        { /* 상태 종료 시 실행 */
            m_player.EndSlide_Anim?.Invoke();
        }
    }
    // -----------------------
    // 예시 상태 (AccelSlide)
    // -----------------------
    public class AccelSlideState : State
    {

        public AccelSlideState(StateMachine fsm, PlayerMovementController m_player) : base(fsm, m_player)
        {

        }

        public override void Enter() 
        { /* 상태 진입 시 실행 */
            m_player.StartSlide_Anim?.Invoke();
            m_player._slideStartTime = Time.time;
            m_player.slideStartDir = m_player.m_LastInput;
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */
            if (m_player.dodgePressed)
            {
                fsm.ChangeState(new DashState(fsm, m_player));
                return;
            }
            else if (!m_player.grounded && Time.fixedTime - m_player.m_TimeLastGrounded > kDelayBeforeInferringJump)
            {
                fsm.ChangeState(new FallState(fsm, m_player));
                return;
            }
            else if (m_player.jumpPressed && m_player.grounded)// && m_player.jumpCnt < m_player.maxJumpCnt)
            {
                fsm.ChangeState(new JumpState(fsm, m_player, 0));
                return;
            }
            else if (m_player.crouchingReleased)
            {
                fsm.ChangeState(new MoveState(fsm, m_player));
                return;
            }
            m_player.AccelSlide(fsm, m_player);
        }
        public override void HandleInput() { }
        public override void Exit() 
        { /* 상태 종료 시 실행 */
            m_player.EndSlide_Anim?.Invoke();
        }
    }
    // -----------------------
    // 예시 상태 (Dash)
    // -----------------------
    public class DashState : State
    {

        public DashState(StateMachine fsm, PlayerMovementController m_player) : base(fsm, m_player)
        {

        }

        public override void Enter() { /* 상태 진입 시 실행 */ }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */

        }
        public override void HandleInput() { }
        public override void Exit() { /* 상태 종료 시 실행 */ }
    }
    // -----------------------
    // 예시 상태 (Climb)
    // -----------------------
    public class ClimbState : State
    {
        Vector3 _endPos;
        int _type;

        public ClimbState(StateMachine fsm, PlayerMovementController m_player, Vector3 endPos, int type) : base(fsm, m_player)
        {
            _endPos = endPos;
            _type = type;
        }

        public override void Enter() 
        { /* 상태 진입 시 실행 */
            m_player.StartCoroutine(m_player.LedgeClimbCoroutine(fsm, m_player, _endPos, _type));
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */

        }
        public override void HandleInput() { }
        public override void Exit() { /* 상태 종료 시 실행 */ }
    }
    // -----------------------
    // 예시 상태 (WallRun)
    // -----------------------
    public class WallRunState : State
    {

        public WallRunState(StateMachine fsm, PlayerMovementController m_player) : base(fsm, m_player)
        {

        }

        public override void Enter() 
        { /* 상태 진입 시 실행 */
            m_player.StartWallRun_Anim?.Invoke();
            m_player.rb.useGravity = false;
        }
        public override void FixedUpdate()
        { /* 매 프레임 검사 */
            //if ((!m_player.wallDetected || !(m_player.playerInput.MoveZ.Value >= 1f) || Vector3.Dot(m_player.wallForwardDir, m_player.m_LastInput) < 0f))
            //{
                //m_isWallRunning = false;
                //rb.useGravity = true;
                //playerController.SwitchState(playerController.moveState);
                if (m_player.dodgePressed)
                {
                    fsm.ChangeState(new DashState(fsm, m_player));
                    return;
                }
                else if (m_player.jumpPressed)// && m_player.jumpCnt < m_player.maxJumpCnt)
                {
                    fsm.ChangeState(new JumpState(fsm, m_player, 1));
                    Debug.Log("야호호");
                    return;
                }
                else if (!m_player.wallDetected || !(m_player.playerInput.MoveZ.Value >= 1f) || Vector3.Dot(m_player.wallForwardDir, m_player.m_LastInput) < 0f || m_player.speedXZ <= 1f)
                {
                    if (!m_player.grounded)
                    {
                        fsm.ChangeState(new FallState(fsm, m_player));
                        return;
                    }
                    else
                    {
                        fsm.ChangeState(new MoveState(fsm, m_player));
                        return;
                    }
                }
            //}
            m_player.WallRun(fsm, m_player);
        }
        public override void HandleInput() { }
        public override void Exit() 
        { /* 상태 종료 시 실행 */
            m_player.EndWallRun_Anim?.Invoke();
            m_player.rb.useGravity = true;
        }
    }
    // -----------------------
    // PlayerFSM 본체
    // -----------------------
    public StateMachine m_fsm { get; private set; }


    private PlayerInputController playerInput;
    private CharacterController characterController;
    private PlayerController playerController;
    private CooldownManager cooldownManager;
    [HideInInspector]public Rigidbody rb { get; private set; }

    [Header("Reference")]
    public GameObject playerModel;
    public Transform camPos;
    public GameObject groundCheckObj;

    [Header("Parameters")]
    public float speed = 15f;
    public float walkSpeed = 5f;
    public float JumpSpeed = 10f;
    public float slidesDampingSpeed = 30f;
    public float slidePower = 20f;
    public float crouchDamping = 0.3f;
    public float groundDrag = 5f;
    public float airDrag = 0f;
    public float slideDrag = 0.1f;
    public float dashDrag = 0.1f;
    public float slopeXspeed = 15f;
    public float wallRunSpeed = 2f;

    public bool grounded { get; private set; }
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
    float _lastSpeedXZ = 0f;
    float _slideStartTime = -1f;
    float _slideMinDuration = 0.25f; // �ּ� 0.15�ʴ� �����̵� ����
    Vector3 slideStartDir;
    float slideSpeed = 25f;

    private float slideFriction = 10f;

    [Header("WallRun")]
    public float wallCheckDistance = 0.4f;
    public float minJumpHeight;
    private bool wallLeft;
    private bool wallRight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    Vector3 wallNormal;
    Vector3 wallForward;
    public bool m_isWallRunning { get; private set; }
    private float wallRunCooldownTimer = 0f;
    private float wallRunCooldownDuration = 0.3f; // 200ms 정도
    public Vector3 wallForwardDir { get; private set; }
    public bool wallDetected { get; private set; }
    public bool isWallRight { get; private set; }

    [Header("Dash")]
    private float _lastDodgeValue;
    private Vector3 dashInput;
    public float dashCooldown = 1.5f;
    bool dodgePressed;

    public bool m_isDashing { get; private set; }

    float rotationSpeed = 3f;

    private Vector3 lastMoveDirection; // ������ �̵� ����

    Vector3 m_LastInput;
    Vector3 m_LastRawInput;
    Vector3 rawInput;

    float cameraYRotation;
    Quaternion inputFrame;
    Vector3 cameraYdir;

    float speedY;
    float speedXZ;
    Vector3 horizontalVelocity;

    bool crouchingReleased;

    bool jumpPressed;
    private int jumpCnt = 0;
    private int maxJumpCnt = 1;
    private bool wasJumped = false;
    private bool m_LastIsJumping = false;
    private bool m_wasCrouching = false;
    private bool crouchingPressed = false;
    private bool m_wasSliding = false;


    public Action StartJump_Anim;
    public Action StartFall_Anim;
    public Action EndJump_Anim;
    public Action Turn180;
    public Action OnTurn180AnimEnd;
    public Action<int> StartLedgeGrap_Anim;
    public Action EndLedgeGrap_Anim;
    public Action<Vector3> StartDash_Anim;
    public Action StartSlide_Anim;
    public Action EndSlide_Anim;
    public Action StartCrouch_Anim;
    public Action EndCrouch_Anim;
    public Action StartWallRun_Anim;
    public Action EndWallRun_Anim;

    [Header("Events")]
    [Tooltip("This event is sent when the player lands after a jump.")]
    public UnityEvent Landed = new();

    [Tooltip("Transition duration (in seconds) when the player changes velocity or rotation.")]
    public float Damping = 1f;

    public float moveDamping = 1f;

    float midAirDampingXZ = 8f;

    // ���� ��ٿ� �ð�
    private float jumpCooldown = 1.2f; // 1�� ���
    private bool isJumpCooling = false;
    private float lastJumpTime = -1.3f; // ������ ���� �ð� �ʱ�ȭ
    private float _lastJumpValue;
    private float _lastCrouchValue;

    private float _lastWallJumpValue;

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

    Vector3 slopeXInput;


    int pakourLayerMask;

    const float kDelayBeforeInferringJump = 0.3f;
    float m_TimeLastGrounded = 0;

    private Coroutine jumpCooldownCoroutine = null;
    private Coroutine ledgeClimbCoroutine = null;
    private Coroutine dashCoroutine = null;

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
        cooldownManager = GetComponent<CooldownManager>();
        //characterController = GetComponent<CharacterController>();
        //slopeLimit = characterController.slopeLimit;
        //animator = playerModel.GetComponent<Animator>();
        //StartCoroutine("Routine");
        pakourLayerMask = 1 << LayerMask.NameToLayer("ParkourStructure");

        m_fsm = new StateMachine();
        m_fsm.ChangeState(new MoveState(m_fsm, this));
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.CurrentState != playerController.uiState)
        {
            if (playerController.CurrentState != playerController.actionState && playerController.CurrentState != playerController.wallRunState)
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
        //Debug.Log(speedXZ);
        //Debug.Log(m_fsm.currentState);
        cameraYRotation = camPos.transform.eulerAngles.y;
        inputFrame = Quaternion.Euler(0, cameraYRotation, 0);
        cameraYdir = inputFrame * Vector3.forward;

        rawInput = new Vector3(playerInput.MoveX.Value, 0, playerInput.MoveZ.Value);

        m_LastInput = inputFrame * rawInput;
        if (m_LastInput.sqrMagnitude > 1)
            m_LastInput.Normalize();

        speedY = rb.linearVelocity.y;
        horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        speedXZ = horizontalVelocity.magnitude;

        //Debug.Log(jumpCnt);
        jumpPressed = _lastJumpValue <= 0 && playerInput.Jump.Value > 0;// && !isJumpCooling;
        dodgePressed = _lastDodgeValue <= 0 && playerInput.Dodge.Value > 0;
        crouchingReleased = playerInput.Crouch.Value <= 0 && _lastCrouchValue > 0;

        if(grounded)
            m_TimeLastGrounded = Time.fixedTime;

        ControlDrag();
        GroundCheck(rb.position, UpDirection);
        WallRunCheck();

        m_fsm.FixedUpdate();

        _lastSpeedXZ = speedXZ;

        _lastDodgeValue = playerInput.Dodge.Value;

        m_LastIsJumping = m_IsJumping;
        _lastJumpValue = playerInput.Jump.Value;

        m_wasSliding = m_IsSliding;
        m_wasCrouching = m_IsCrouching;
        _lastCrouchValue = playerInput.Crouch.Value;
        /*
        if (playerController.CurrentState != playerController.uiState)
        {
            jumpPressed = _lastJumpValue <= 0 && playerInput.Jump.Value > 0 && !isJumpCooling;
            dodgePressed = _lastDodgeValue <= 0 && playerInput.Dodge.Value > 0;

            //Debug.Log("����ӵ�: " + rb.linearVelocity);

            Vector3 projected = Vector3.ProjectOnPlane(-UpDirection, GroundHitNormal);
            if (projected.sqrMagnitude < 1e-6f)  // �ʹ� ������
            {
                slopeDir = UpDirection; // �Ǵ� ������ �⺻��
            }
            else
            {
                slopeDir = projected.normalized;
            }
            ControlDrag();
            GroundCheck(rb.position, UpDirection);
            WallRunCheck();
            LedgeGrap();
            WallRun();
            //Debug.Log(cooldownManager.GetRemainingTime("Dash"));
            if (!cooldownManager.IsOnCooldown("Dash"))
            {
                Dash();
            }
            if (playerController.CurrentState != playerController.actionState && playerController.CurrentState != playerController.wallRunState)
            {
                Move();
                ProcessJump();
                Slide();
            }
            _lastDodgeValue = playerInput.Dodge.Value;

            m_LastIsJumping = m_IsJumping;
            _lastJumpValue = playerInput.Jump.Value;

            m_wasSliding = m_IsSliding;
            m_wasCrouching = m_IsCrouching;
            _lastCrouchValue = playerInput.Crouch.Value;
        }
        */
    }
    void ControlDrag()
    {
        /*
        if (m_isDashing)
            rb.linearDamping = dashDrag;
        else
        {
            
            if (!(fsm.currentState is JumpState))
            {
                if (fsm.currentState is SlideState)
                    rb.linearDamping = slideDrag;
                else
                    rb.linearDamping = groundDrag;
            }
            else
            {
                if (fsm.currentState is WallRunState)
                    rb.linearDamping = groundDrag;
                else
                    rb.linearDamping = airDrag;
            }
            
        }*/
        switch (m_fsm.currentState)
        {
            case MoveState moveState:
                rb.linearDamping = groundDrag;
                break;
            case CrouchState crouchState:
                rb.linearDamping = groundDrag;
                break;
            case JumpState jumpState:
                rb.linearDamping = airDrag;
                break;
            case FallState fallState:
                rb.linearDamping = airDrag;
                break;
            case AccelSlideState accelSlideState:
                rb.linearDamping = groundDrag;
                break;
            case SlideState accelSlideState:
                rb.linearDamping = slideDrag;
                break;
            case DashState accelSlideState:
                rb.linearDamping = dashDrag;
                break;
            case WallRunState wallRunState:
                rb.linearDamping = groundDrag;
                break;
        }
    }
    private void Move(StateMachine fsm, PlayerMovementController m_player)
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

        var rawXInput = new Vector3(playerInput.MoveX.Value, 0f, 0f);
        var lastXInput = inputFrame * rawXInput;
        if (lastXInput.sqrMagnitude > 1)
            lastXInput.Normalize();

        Vector3 projected = Vector3.Project(lastXInput, slopeRight);
        slopeXInput = projected;

        Vector3 slopeXVelocity = slopeXInput * slopeXspeed;
        
        deltaPosition = m_LastInput * speed * Time.fixedDeltaTime;
        var slopeXDeltaPosition = slopeXInput * 10f * Time.fixedDeltaTime;

        // deltaPosition �� deltaVelocity
        Vector3 inputVelocity = m_LastInput * speed;

        // slope �¿� �̵�

        if (grounded)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(inputVelocity, GroundHitNormal).normalized * inputVelocity.magnitude;

            //Debug.Log();

            float alignment = Vector3.Dot(slopeDirection.normalized, slopeDir);
            float t = (alignment + 1f) * 0.5f;
            float slopeBoost = Mathf.Lerp(slopeMinBoost, slopeMaxBoost, t);

            Vector3 targetVelocity;

            targetVelocity = slopeDirection;// * slopeBoost;
            rb.AddForce(targetVelocity, ForceMode.Acceleration);
            /*
            if (m_IsAccelSliding)
            {

            }
            else if (m_IsSliding)
            {

            }
            else if (m_IsCrouching)
            {
                targetVelocity = slopeDirection * crouchDamping;// * slopeBoost * crouchDamping;
                rb.AddForce(targetVelocity, ForceMode.Acceleration);
            }
            else ///일반 이동 시
            {
                targetVelocity = slopeDirection;// * slopeBoost;
                rb.AddForce(targetVelocity, ForceMode.Acceleration);
                //Debug.Log("slopeDirection: " + slopeDirection + "slopeBoost: " + slopeBoost);
            }

            // ���� velocity ���� (Y���� ����)
            //rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
            
        }
        else
        {
            if (m_isWallRunning)
            {

            }
            else
            {
                var v = GetDirDiff(rb.linearVelocity, m_LastInput);
                rb.AddForce(v * (speed / 5) * m_LastInput, ForceMode.Acceleration);
            } ////고안 필요
            */
        }
    }
    void Crouch()
    {
        var rawInput = new Vector3(playerInput.MoveX.Value, 0, playerInput.MoveZ.Value);
        m_LastInput = inputFrame * rawInput;                            
        if (m_LastInput.sqrMagnitude > 1)
            m_LastInput.Normalize();



        Vector3 slopeRight;

        if (Vector3.Angle(GroundHitNormal, Vector3.up) == 0f)
        {
            slopeRight = camPos.transform.right;
        }
        else
        {
            slopeRight = Vector3.Cross(GroundHitNormal, slopeDir).normalized;
        }

        var rawXInput = new Vector3(playerInput.MoveX.Value, 0f, 0f);
        var lastXInput = inputFrame * rawXInput;
        if (lastXInput.sqrMagnitude > 1)
            lastXInput.Normalize();

        Vector3 projected = Vector3.Project(lastXInput, slopeRight);
        slopeXInput = projected;

        Vector3 slopeXVelocity = slopeXInput * slopeXspeed;

        deltaPosition = m_LastInput * speed * Time.fixedDeltaTime;
        var slopeXDeltaPosition = slopeXInput * 10f * Time.fixedDeltaTime;

        // deltaPosition �� deltaVelocity
        Vector3 inputVelocity = m_LastInput * speed;

        // slope �¿� �̵�

        if (grounded)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(inputVelocity, GroundHitNormal).normalized * inputVelocity.magnitude;

            //Debug.Log();

            float alignment = Vector3.Dot(slopeDirection.normalized, slopeDir);
            float t = (alignment + 1f) * 0.5f;

            Vector3 targetVelocity;


            targetVelocity = slopeDirection * crouchDamping;
            rb.AddForce(targetVelocity, ForceMode.Acceleration);
            
        }
    }

    float GetDirDiff(Vector3 beforeJumpDir, Vector3 inputDir)
    {
        // xz 평면에서만 비교
        Vector2 a = new Vector2(beforeJumpDir.x, beforeJumpDir.z).normalized;
        Vector2 b = new Vector2(inputDir.x, inputDir.z).normalized;

        if (a == Vector2.zero || b == Vector2.zero)
            return 0f; // 둘 중 하나라도 입력이 없으면 유사도 없음

        // 코사인 유사도 (dot product): 1 (같은 방향), 0 (직각), -1 (반대 방향)
        float dot = Vector2.Dot(a, b);

        // dot == 1 → 0 반환, dot == -1 → 1 반환, dot == 0 → 0.5 반환되도록 변환
        float result = Mathf.Clamp01(1f - dot);

        return result; // 0~1 사이의 값
    }
    void AccelSlide(StateMachine fsm, PlayerMovementController m_player)
    {
        bool slideDurationPassed = Time.fixedTime - _slideStartTime >= _slideMinDuration;

        var slopePosition = Vector3.ProjectOnPlane(slideStartDir, GroundHitNormal).normalized;

        //if (dodgePressed || jumpPressed || crouchingReleased || (Mathf.Abs(speedY) < 0.1f && (!m_IsAccelSliding && m_IsSliding && speedXZ < 0.25f)) || Vector3.Angle(cameraYdir, horizontalVelocity) > 130f)

        if (slideDurationPassed)
        {
            fsm.ChangeState(new SlideState(fsm, m_player, slideStartDir));
        }

        rb.linearVelocity = slopePosition * slideSpeed;
    }
    void Slide(StateMachine fsm, PlayerMovementController m_player, Vector3 slideDir)
    {
        Vector3 v = rb.linearVelocity;

        // XZ 속도의 크기만 계산
        //float speedXZ = new Vector3(vel.x, 0, vel.z).magnitude;

        // 고정된 방향으로 다시 할당
        Vector3 velocityXZ = slideDir * speedXZ;

        // Y는 그대로
        rb.linearVelocity = new Vector3(velocityXZ.x, v.y, velocityXZ.z);
    }
    /*
    void Slide(StateMachine fsm, PlayerMovementController m_player)
    {
        float cameraYRotation = camPos.transform.eulerAngles.y;
        var inputFrame = Quaternion.Euler(0, cameraYRotation, 0);

        var cameraYdir = inputFrame * Vector3.forward;

            if (playerInput.Crouch.Value > 0)
            {
                if (grounded && !m_IsJumping)
                {
                    m_IsCrouching = true;
                    bool crouchingStarted = m_IsCrouching && !m_wasCrouching;
                    //Debug.Log("����");
                    if (playerInput.MoveZ.Value > 0 && crouchingStarted)
                    {
                        Debug.Log("슬라이딩 시작");
                        m_IsSliding = true;
                        _slideStartTime = Time.time;


                        //slideStartDir = rb.linearVelocity.normalized;
                        slideStartDir = m_LastInput;

                        m_IsAccelSliding = true;
                    }

                }
                else
                {
                    m_IsCrouching = false;
                    //m_IsSliding = false;
                }
                
            }
            else
            {
                m_IsCrouching = false;
                //m_IsSliding = false;
            }
            
        //}
        bool slidingEnded = !m_IsSliding && m_wasSliding;
        bool crouchingEnded = !m_IsCrouching && m_wasCrouching;
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speedXZ = horizontalVelocity.magnitude;


        float speedY = rb.linearVelocity.y;

        bool isSlowingDown = speedXZ < _lastSpeedXZ;


        if (!m_IsAccelSliding)
        {
            //이미 멈췄으면 아무것도 하지 않음
            //Debug.Log(GetDirDiff(rb.linearVelocity, m_LastInput) > 0.9f);
        }
        else
        {
            bool slideDurationPassed = Time.time - _slideStartTime >= _slideMinDuration;

            //if (crouchingEnded || slideDurationPassed || Vector3.Angle(cameraYdir, horizontalVelocity) > 130f)// || GetDirDiff(rb.linearVelocity, m_LastInput) > 0.9f)
            //{
            //    m_IsAccelSliding = false;;
            //}
            var slopePosition = Vector3.ProjectOnPlane(slideStartDir, GroundHitNormal).normalized;
            rb.linearVelocity = slopePosition * slideSpeed;
        }
        //Debug.Log(m_isDashing);
        
        if (dodgePressed || jumpPressed || crouchingReleased || (Mathf.Abs(speedY) < 0.1f && (!m_IsAccelSliding && m_IsSliding && speedXZ < 0.25f)) || Vector3.Angle(cameraYdir, horizontalVelocity) > 130f)// || (GetDirDiff(rb.linearVelocity, m_LastInput) > 0.9f && m_LastInput.magnitude > 0.05f))
        {

            //if (m_IsSliding)
            //{
                Debug.Log("슬라이딩 끝");
                m_IsSliding = false;
            m_IsAccelSliding = false;
                if (grounded && !m_IsJumping)
                {

                }
            //}
        }
        _lastSpeedXZ = speedXZ;
    }
    */
    bool IsWithinBounds(Vector3 pos, float maxValue)
    {
        return Mathf.Abs(pos.x) <= maxValue && Mathf.Abs(pos.z) <= maxValue;
    }
    void Jump(StateMachine fsm, PlayerMovementController m_player, int jumpType)
    {
        //bool justLanded = false;
        //var now = Time.time;

        // Process jump command
        //if (jumpPressed && jumpCnt < maxJumpCnt)
        //if(jumpCnt < maxJumpCnt)
        //{
        //m_IsJumping = true;
        if (jumpType == 0)
        {
            float jumpVelocity = Mathf.Sqrt(2 * JumpSpeed * Mathf.Abs(Physics.gravity.y));

            Vector3 v = rb.linearVelocity;
            v.y = jumpVelocity;
            rb.linearVelocity = v;
            //lastJumpTime = now;
            //if (grounded)
            //{
            //}
            //Debug.Log("캬");
            jumpCnt++;
            wasJumped = true;
        }
        else if (jumpType == 1)
        {
            rb.AddForce(m_player.wallNormal * 350f + UpDirection * 250f, ForceMode.Impulse);
            //rb.useGravity = true;
            //m_isWallRunning = false;
            //playerController.SwitchState(playerController.moveState);

            //wallRunCooldownTimer = wallRunCooldownDuration; // 쿨다운 시작

            //StartJump_Anim?.Invoke();
            jumpCnt++;
            wasJumped = true;
        }
        //}

        // If we are falling, assume the jump pose
        //if (!grounded && now - m_TimeLastGrounded > kDelayBeforeInferringJump && jumpCnt < maxJumpCnt)
        //{
            //m_IsJumping = true;
            //if (!wasJumped)
            //    jumpCnt++;

        //}
        //if (m_IsJumping && !m_LastIsJumping)
        //{
            //StartFall?.Invoke();
            //grounded = false;
        //}

        /*
        if (grounded && rb.linearVelocity.y <= 0f)
        {
            m_TimeLastGrounded = Time.time;

            

            jumpCnt = 0;
            // If we were jumping, complete the jump
            //if (m_IsJumping)
            //{
            EndJump?.Invoke();
            Debug.Log("체인지");
            //justLanded = true;
            Landed.Invoke();
            //m_IsJumping = false;
            if (playerInput.Crouch.Value > 0f && playerInput.MoveZ.Value > 0f)
                fsm.ChangeState(new AccelSlideState(fsm, m_player));
            else    
                fsm.ChangeState(new MoveState(fsm, m_player));
                
                
            //}
        }*/
        //return justLanded;
    }
    void Fall(StateMachine fsm, PlayerMovementController m_player)
    {
        //m_IsJumping = true;
        //공중 이동동작 넣기
        //Vector3 inputDir = new Vector3(inputX, 0, inputZ).normalized;

        // 현재 XZ 속도
        //Vector3 velXZ = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        //float speed = velXZ.magnitude;
        /*
        // 목표 최대 속도
        float maxAirSpeed = 8f;

        // 속도가 maxAirSpeed에 가까울수록 힘을 줄이기
        float dampingFactor = Mathf.Clamp01(1f - (speedXZ / maxAirSpeed));

        // 최종 힘 계산
        Vector3 force = m_LastInput * speed * dampingFactor;

        rb.AddForce(force, ForceMode.Acceleration);
        */
        //rb.AddForce(speed * m_LastInput, ForceMode.Acceleration);
    }
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
    void LedgeGrapCheck(StateMachine fsm, PlayerMovementController m_player)    ///렛지 확인 및 확인 완료 시 climb 상태 전이
    {
        //if ((playerController.CurrentState == playerController.moveState || playerController.CurrentState == playerController.idleState) && playerController.CurrentState != playerController.actionState)
        //{
            //if (m_IsJumping)
            //{
                //bool jumpPressed = _lastJumpValue <= 0 && playerInput.Jump.Value > 0;
                //if (jumpPressed)
                //{
                    RaycastHit hit;

                    Vector3 origin = transform.position + Vector3.up * 1f;


                    Vector3 forward = new Vector3(modelTransform.forward.x, transform.forward.y, modelTransform.forward.z);

                    if (Physics.Raycast(origin, forward, out hit, 1f, pakourLayerMask))
                    {
                        Vector3 ledgeCheck;

                        for (float iter = 0.1f; iter <= 2f; iter += 0.1f)
                        {
                            ledgeCheck = hit.point + Vector3.up * iter - forward * 0.05f;
                            if (!Physics.Raycast(ledgeCheck, forward, 1f, pakourLayerMask))
                            {
                                //Debug.Log(ledgeClimbCoroutine == null);
                                //if (ledgeClimbCoroutine == null)
                                //{
                                    //Debug.Log("렛지그랩시작");
                                    //ledgeClimbCoroutine = StartCoroutine(LedgeClimbCoroutine(fsm, m_player, ledgeCheck + forward * 0.1f, 0));// + (Vector3.up * 0.2f)));
                                    fsm.ChangeState(new ClimbState(fsm, m_player, ledgeCheck + forward * 0.1f, 0));
                                    
                                //}
                                break;
                            }
                        }
                    }
                //}
            //}
        //}
    }
    IEnumerator LedgeClimbCoroutine(StateMachine fsm, PlayerMovementController m_player, Vector3 endPos, int type) 
    {
        //fsm.ChangeState(new ClimbState(fsm, m_player));

        //m_CanMove = false;

        //playerController.SwitchState(playerController.actionState);

        rb.linearVelocity = Vector3.zero;

        rb.isKinematic = true;

        Vector3 start = transform.position;

        //float duration = 0.25f;
        float duration = 0.25f;
        if (type == 1)
        {
            duration = 0.2f;
        }

        StartLedgeGrap_Anim?.Invoke(type);

        float height = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            //코루틴 탈출 조건
            //while (playerController.CurrentState == playerController.uiState)
            //{
            //    yield return null;
            //}

            float t = elapsed / duration;

            Vector3 horizontal = Vector3.Lerp(start, endPos, t);

            float arc = 4 * height * t * (1 - t); 

            transform.position = horizontal + Vector3.up * arc;

            elapsed += Time.fixedDeltaTime;

            yield return null;
        }

        transform.position = endPos;

        rb.isKinematic = false;
        rb.useGravity = true;

        //playerController.SwitchState(playerController.idleState);
        EndLedgeGrap_Anim?.Invoke();

        ledgeClimbCoroutine = null;
        //Debug.Log("나여");
        if(grounded)
            fsm.ChangeState(new MoveState(fsm, m_player));
        else
            fsm.ChangeState(new FallState(fsm, m_player));
        yield break;

    }


    IEnumerator JumpCooldownCoroutine()
    {
        isJumpCooling = true;
        yield return new WaitForSeconds(0.2f);
        isJumpCooling = false;
        jumpCooldownCoroutine = null; 
    }


    private void Dash()
    {
        if ((playerController.CurrentState == playerController.moveState || playerController.CurrentState == playerController.idleState || playerController.CurrentState == playerController.wallRunState) && playerController.CurrentState != playerController.actionState)
        {
            if (dodgePressed)
            {
                dashInput = m_LastInput;
                if(!m_isDashing)
                    StartCoroutine(DashCoroutine(dashInput));
            }
        }
    }
    private IEnumerator DashCoroutine(Vector3 dashInput)
    {
        Debug.Log("머");
        m_isDashing = true;

        if (dashInput == Vector3.zero)
        {
            float cameraYRotation = camPos.transform.eulerAngles.y;
            var inputFrame = Quaternion.Euler(0, cameraYRotation, 0);
            dashInput = inputFrame * new Vector3(0f, 0f, 1f);
        }
        playerController.SwitchState(playerController.actionState);

        rb.linearVelocity = Vector3.zero;

        rb.useGravity = false;


        float duration = 0.5f; // 0.5f // ��ü �̵� �ð�


        StartDash_Anim?.Invoke(dashInput);

        float elapsed = 0f;

        rb.linearVelocity = Vector3.zero;

        rb.AddForce(dashInput * 50f, ForceMode.VelocityChange);

        while (elapsed < duration)
        {
            while (playerController.CurrentState == playerController.uiState) ////중단
            {
                yield return null;
            }

            Vector3 dashDirection = Vector3.ProjectOnPlane(dashInput, GroundHitNormal).normalized;

            //rb.linearVelocity = dashDirection * 40f;

            elapsed += Time.deltaTime;
            yield return null;
        }

        //수정필요 ? addforce로 변경?

        rb.useGravity = true;

        m_isDashing = false;

        playerController.SwitchState(playerController.moveState);

        cooldownManager.StartCooldown("Dash", dashCooldown);
    }

    private void WallRunCheck()
    {
        wallRight = Physics.Raycast(transform.position + transform.up * 1f, modelTransform.right, out rightWallHit, wallCheckDistance, GroundLayers, QueryTriggerInteraction.Ignore);
        wallLeft = Physics.Raycast(transform.position + transform.up * 1f, -modelTransform.right, out leftWallHit, wallCheckDistance, GroundLayers, QueryTriggerInteraction.Ignore);
        if (wallDetected)
        {
            isWallRight = wallRight ? true : false;
        }
        wallDetected = wallRight || wallLeft;

        wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((camPos.forward - wallForward).magnitude > (camPos.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        wallForwardDir = wallForward;
    }

    private void WallRun(StateMachine fsm, PlayerMovementController m_player)
    {
        //if (wallRunCooldownTimer > 0f)
        //    wallRunCooldownTimer -= Time.deltaTime;


        //if (playerController.CurrentState == playerController.moveState || playerController.CurrentState == playerController.idleState || playerController.CurrentState == playerController.wallRunState)
        //{
            //bool jumpPressed = _lastJumpValue <= 0 && playerInput.Jump.Value > 0;
            /*
            wallDetected = wallRight || wallLeft;

            Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            if ((camPos.forward - wallForward).magnitude > (camPos.forward - -wallForward).magnitude)
                wallForward = -wallForward;

            wallForwardDir = wallForward;
            */
        // 벽 점프: 벽달리기 중에 점프키 눌림
        /*
        if (m_isWallRunning && jumpPressed)
        {
            rb.AddForce(wallNormal * 150f + UpDirection * 200f, ForceMode.Impulse);
            rb.useGravity = true;
            m_isWallRunning = false;
            playerController.SwitchState(playerController.moveState);

            wallRunCooldownTimer = wallRunCooldownDuration; // 쿨다운 시작

            StartJump_Anim?.Invoke();
            jumpCnt++;
            wasJumped = true;
        }
        */
        //벽점프는 점프에 넣고 점프 상태로 전이시 벽점프 또는 일반 점프인지 구별할 것
        // 벽 달리기 시작 조건
        //else if (!m_isWallRunning && m_IsJumping && wallDetected && playerInput.MoveZ.Value >= 1f && wallRunCooldownTimer <= 0f)
        //{
        //m_isWallRunning = true;
        //rb.useGravity = false;
        //playerController.SwitchState(playerController.wallRunState);
        //}
        // 벽 달리기 중단 조건
        //else if (m_isWallRunning && (!m_IsJumping || !wallDetected || !(playerInput.MoveZ.Value >= 1f) || Vector3.Dot(wallForwardDir ,m_LastInput) < 0f))
        /*
        if (m_isWallRunning && (!wallDetected || !(playerInput.MoveZ.Value >= 1f) || Vector3.Dot(wallForwardDir, m_LastInput) < 0f))
        {
            //m_isWallRunning = false;
            //rb.useGravity = true;
            //playerController.SwitchState(playerController.moveState);
            if (!grounded)
            {
                m_fsm.ChangeState(new FallState(fsm, m_player));
                return;
            }
            else
            {
                m_fsm.ChangeState(new MoveState(fsm, m_player));
                return;
            }
        }
        */
            //if (m_isWallRunning)
            //{
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                rb.AddForce(wallForward * wallRunSpeed, ForceMode.Acceleration);
                rb.AddForce(-wallNormal * 5f, ForceMode.Acceleration);
            //}

            //Debug.Log("m_isWallRunning: " + m_isWallRunning);
            //_lastWallJumpValue = playerInput.Jump.Value;
            
        //}


        
    }
}