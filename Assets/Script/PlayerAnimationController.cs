using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.HID;

public class PlayerAnimationController : MonoBehaviour
{
    public GameObject player;

    public Animator animator;               // 애니메이터   

    PlayerController playerController;
    CharacterController charController; // CharacterController
    Rigidbody rb;
    PlayerInputController playerInput;
    PlayerMovementController playerMovementController;

    public KatanaController katana;
    public Transform camPos;

    public bool leftGrip_IKActive = false;
    public bool saya_IkActive = false;
    public bool isEquipingNow = false;          ///칼을 차고 넣는 애니메이션이 진행중인가?
    public bool isLookAt = false;

    private IEnumerator turnCoroutine;
    private float turnTime = 0.5f;

    public Transform upperbody;

    public Transform leftHandObj = null;
    public Transform leftHandGripPoint = null;
    public Transform leftHandGripPoint_Equip = null;
    public Transform leftElbowHint = null;

    public Transform lookPoint;

    public Vector3 initialHipRotationEuler;  // 인스펙터에서 실시간으로 수정 가능한 초기 Hip 회전값 (Euler Angles)
    private Quaternion initialHipRotation;  // 초기 Hip 회전값 (Quaternion으로 변환하여 저장)

    public Vector3 fixedSpineRotation = new Vector3(3.514341f, 56.0466f, 10.38653f);

    private float _verticalViewAngle;
    private float _horizontalViewAngle;

    private bool rotateSpine = false;

    private Quaternion hipOriginalYawRotation = Quaternion.Euler(0f, 0f, 0f);
    public float maxYawAngle = 40f;  // 최댓값 (40도)
    public float rotationSmoothSpeed = 5f; // 부드러운 회전 속도 조절

    float minValue = -40f;
    float maxValue = 40f;               //좌우 대각선 전진시 hip이 틀어지는 y축 회전 각도에 따라 수정

    bool isStrip = false;

    private int cnt = 0;                                                                                                    ///////////왼쪽 전진, 오른쪽 전진 시 몸통 회전 조정하여 몸통이 정면 주시하도록 만들기 
                                                                                                                            ///////////전진시 왼손 칼 손잡이에 부착 및 좌우전진 시 문제 해결
    public MultiAimConstraint spineIK;

    [System.Serializable]
    public struct Bone
    {
        public Transform spine;
        public Transform head;
        public Transform hip;
    }
    [SerializeField] public Bone MainBone;

    Transform hipBone;

    bool a = false;
    protected struct AnimationParams
    {
        public bool IsWalking;
        public bool IsRunning;
        public bool IsJumping;
        public bool IsEquiping;
        public bool activeIK;
        public bool JumpTriggered;
        public bool FallTriggered;
        public bool LandTriggered;
        public Vector3 Direction; // normalized direction of motion
        public float MotionScale; // scale factor for the animation speed
        public float JumpScale; // scale factor for the jump animation
    }
    AnimationParams m_AnimationParams;

    [SerializeField] private float ikTransitionSpeed = 5.0f; // IK 전환 속도 (조정 가능)
    private float currentIKWeight = 0.0f; // 현재 IK Weight 값
    private float currentLeftGripIKWeight = 0.0f;
    private float currentLeftHandIKWeight = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = player.GetComponent<PlayerController>();
        charController = player.GetComponent<CharacterController>();
        playerInput = player.GetComponent<PlayerInputController>();
        playerMovementController = player.GetComponent<PlayerMovementController>();
        rb = player.GetComponent<Rigidbody>();

        playerInput.OnRightClickedToAim += ToAimCamera;
        playerInput.OnRightClickedToFreeLook += ToFreeLookCamera;
        playerInput.OnZButtonPressed += EquipUnequip;

        playerMovementController.Turn180 += PlayAnim_Turn180;
        playerMovementController.StartLedgeGrap_Anim += PlayAnim_LedgeClimb;
        playerMovementController.EndLedgeGrap_Anim += EndAnim_LedgeClimb;
        playerMovementController.StartDash_Anim += PlayAnim_Dash;
        playerMovementController.StartSlide_Anim += PlayAnim_Slide;
        playerMovementController.EndSlide_Anim += EndAnim_Slide;
        playerMovementController.StartCrouch_Anim += PlayAnim_Crouch;
        playerMovementController.EndCrouch_Anim += EndAnim_Crouch;
        playerMovementController.StartWallRun_Anim += PlayAnim_WallRun;
        playerMovementController.EndWallRun_Anim += EndAnim_Wallrun;

        //playerController.Turn180 += PlayAnim_Turn180;

        playerMovementController.StartJump_Anim += () => m_AnimationParams.JumpTriggered = true;
        playerMovementController.StartFall_Anim += () => m_AnimationParams.FallTriggered = true;
        playerMovementController.EndJump_Anim += () => m_AnimationParams.LandTriggered = true;

        PlayerIdleAnim_Inter.OnUnequiping += UnEquip;

        if (animator.avatar == null)
        {
            Debug.Log("Null avatar");
        }
        else if (!animator.avatar.isValid)
        {
            Debug.Log("Invalid avatar");
        }
        else if (!animator.avatar.isHuman)
        {
            Debug.Log("GetBoneTransform cannot be used on a generic avatar");
        }
        else if (animator.GetBoneTransform(HumanBodyBones.Spine) == null)
        {
            Debug.Log("FFFFFFFFFFFFFFFFFFF");
        }

        //spineBone = animator.GetBoneTransform(HumanBodyBones.Spine); // 척추 본 가져오기
        //hipBone = animator.GetBoneTransform(HumanBodyBones.Hips);
    }

    private void Update()
    {
        AnimatorStateInfo stateInfo0 = animator.GetCurrentAnimatorStateInfo(0); // 0은 레이어 인덱스
        AnimatorStateInfo stateInfo1 = animator.GetCurrentAnimatorStateInfo(1); // 1은 레이어 인덱스
        
        if (playerController.CurrentState == playerController.moveState)
        {
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }

        if ((stateInfo0.IsName("Equip_Walk") && playerInput.MoveZ.Value > 0f))
        {
            if (!m_AnimationParams.activeIK)
            {
                m_AnimationParams.activeIK = true;
            }
        }
        else
        {
            if (m_AnimationParams.activeIK)
            {
                m_AnimationParams.activeIK = false;
            }
        }

        if (stateInfo0.IsName("Idle") || stateInfo0.IsName("Walk")) //|| (stateInfo0.IsName("Equip_Walk") && playerController.MoveZ.Value > 0f)) ///Idle상태, walk상태, walk_Equip인데 playerController.MoveZ.Value > 0일때만 true
        {
            if (!saya_IkActive)
            {
                saya_IkActive = true;
                leftGrip_IKActive = false;
            }
        }
        else
        {
            if (saya_IkActive)
            {
                saya_IkActive = false;
                leftGrip_IKActive = false;
            }
        }

        
    }
    
    // Update is called once per frame
    void LateUpdate()
    {
        //rotateSpine = animator.GetBool("RotateSpine");
        /*
        if (playerMovementController.m_isWallRunning)
        {
            //transform.rotation = Quaternion.Euler(playerMovementController.wallForwardDir.x, 0f , playerMovementController.wallForwardDir.z);
        }
        */
        //else
        //{
        //animator.SetBool("WallRunning", playerMovementController.m_isWallRunning);

        //animator.SetBool("Sliding", playerMovementController.m_IsSliding);

        //animator.SetBool("Crouching", playerMovementController.m_IsCrouching);

        if (playerMovementController.isWallRight) 
        {
            animator.SetFloat("WallDetected", 1f); 
        }
        else
        {
            animator.SetFloat("WallDetected", 0f);
        }

        if (playerMovementController.m_fsm.currentState is PlayerMovementController.WallRunState)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(playerMovementController.wallForwardDir.x, 0f, playerMovementController.wallForwardDir.z).normalized);
        }
        //else if(playerMovementController.m_fsm.currentState is PlayerMovementController.SlideState && !(playerMovementController.m_fsm.currentState is PlayerMovementController.JumpState || playerMovementController.m_fsm.currentState is PlayerMovementController.FallState))
        else if(playerMovementController.m_fsm.currentState is PlayerMovementController.AccelSlideState || playerMovementController.m_fsm.currentState is PlayerMovementController.SlideState)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(rb.linearVelocity.normalized.x, 0f, rb.linearVelocity.normalized.z)); //이동방향 고정
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, camPos.eulerAngles.y, 0f);
        }
        //}

        //transform.rotation = Quaternion.Euler(0f, camPos.eulerAngles.y, 0f);

        //if ((playerInput.MoveX.Value != 0f) || (playerInput.MoveZ.Value != 0f)) //////wasd 입력이 하나라도 있으면

        animator.SetFloat("Pos X", playerInput.MoveX.Value, 0.1f, Time.deltaTime);
        animator.SetFloat("Pos Y", playerInput.MoveZ.Value, 0.1f, Time.deltaTime);

        if (m_AnimationParams.JumpTriggered)
        {
            //animator.SetTrigger("Jump");
            animator.SetTrigger("Jump");
        }
        if (m_AnimationParams.FallTriggered)
        {
            animator.SetBool("Falling", true);
        }
        if (m_AnimationParams.LandTriggered)
        {
            //animator.SetTrigger("Land");
            animator.SetBool("Falling", false);
            animator.SetBool("Jumping", false);
            //StartCoroutine("LandCoroutine");
        }
        m_AnimationParams.JumpTriggered = false;
        m_AnimationParams.FallTriggered = false;
        m_AnimationParams.LandTriggered = false;

        

        RotateSpine();

        //player.transform.rotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
    }
    private void FixedUpdate()
    {
        /*
        if (playerController.CurrentState == playerController.moveState)
        {
            
        }
        else if (playerController.CurrentState == playerController.idleState)
        {
            var currentForward = transform.forward;

            var angle = Vector3.Angle(currentForward, camPos.forward);

            if (MathF.Abs(angle) > 50)
            {
                if (turnCoroutine != null) StopCoroutine(turnCoroutine);
                turnCoroutine = BodyTurn(angle);
                //StartCoroutine(turnCoroutine);
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f), 10f * Time.deltaTime);
            }
        }
        */
    }

    private void RotateSpine()
    {
        bool rotSpine = animator.GetBool("RotSpine");

        _verticalViewAngle = Vector3.SignedAngle(transform.up, camPos.up, camPos.right); ///campos와 transform의 수직 각도 차이

        var verticalRotate = Quaternion.AngleAxis(_verticalViewAngle, transform.right);

        var finalVerticalRotation = verticalRotate * MainBone.spine.rotation;


        Quaternion finalHorizontalRotation_Action = Quaternion.AngleAxis(camPos.eulerAngles.y, transform.up);

        var verticalRotate_Action = Quaternion.AngleAxis(_verticalViewAngle + 35f, camPos.right);

        var finalRotation_Action = verticalRotate_Action * finalHorizontalRotation_Action;




        var horizontalRotate = Quaternion.AngleAxis(camPos.eulerAngles.y, transform.up);

        var finalHorizontalRotation = horizontalRotate;

        var boneForwardOffsetCalculate = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + camPos.rotation.eulerAngles.y, 0);

        var verticalRotate1 = Quaternion.AngleAxis(_verticalViewAngle, camPos.right);

        var finalRotation = verticalRotate1 * finalHorizontalRotation;// * MainBone.spine.rotation;


        var ang = camPos.rotation * Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f));

        var angle = ang * MainBone.spine.rotation;// * Quaternion.Inverse(transform.rotation); ////플레이어 모델 기준으로 회전함 수정 필요
        

        ////액션 상태, 공격 애니메이션, 월런 등 애니메이션 및 상태 조건에 따라 처리 방법 다르게 적용

        //if (playerController.CurrentState != playerController.actionState)
        //{
            if(rotSpine) //공격 시 강제 해당 방향 회전 및 애니메이션에 맞춰 회전 각도 보정
            {
                MainBone.spine.rotation = Quaternion.Slerp(MainBone.spine.rotation, finalRotation_Action, Time.deltaTime * 350f);
            }
            else
            {
                if(playerMovementController.m_fsm.currentState is PlayerMovementController.ClimbState || playerMovementController.m_fsm.currentState is PlayerMovementController.WallRunState || playerMovementController.m_fsm.currentState is PlayerMovementController.AccelSlideState || playerMovementController.m_fsm.currentState is PlayerMovementController.SlideState)
                {
                    //MainBone.spine.rotation = finalRotation_Action;
                    //아무것도 안 함
                }
                else
                {
                    MainBone.spine.rotation = finalVerticalRotation;
                }
            }
        //}

        //Debug.Log(playerMovementController.m_isWallRunning);
        //Debug.Log("수평회전: " + horizontalRotate.eulerAngles + " 척추: " + MainBone.spine.rotation.eulerAngles + " 캠포스: " + camPos.eulerAngles);
        /*
        if (true)//playerController.CurrentState == playerController.wallRunState)
        {
            MainBone.spine.rotation = camPos.rotation * MainBone.spine.rotation * Quaternion.Euler(0f, 35.4f, 0f);//angle;
            Debug.Log("camPos" + camPos.rotation.eulerAngles.y + " 메인본 y: " + MainBone.spine.rotation.eulerAngles.y + " 곱: " + camPos.rotation * MainBone.spine.rotation);
        }
        else if (playerController.CurrentState == playerController.moveState || playerController.CurrentState == playerController.idleState)
        {
            MainBone.spine.rotation = finalVerticalRotation;
        }
        */
    }

    IEnumerator BodyTurn(float angle)
    {
        float t = 0f;

        var startRot = player.transform.rotation;

        while (t < turnTime)
        {
            t += Time.deltaTime;

            player.transform.rotation = Quaternion.Slerp(startRot, Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0), t / turnTime);

            yield return null;
        }

        turnCoroutine = null;
    }

    private void ToAimCamera()
    {
        animator.SetBool("Aiming", true);
    }
    private void ToFreeLookCamera()
    {
        animator.SetBool("Aiming", false);
    }
    private void PlayAnim_Turn180()
    {
        playerMovementController.m_CanTurn180 = false;
        //animator.applyRootMotion = true;
        animator.SetTrigger("Turn180");
    }
    private void PlayAnim_LedgeClimb(int type) ///type = 0: 머리높이, 1: 가슴높이
    {
        //animator.applyRootMotion = true;
        if (type == 0)
        {
            animator.SetTrigger("LedgeClimb");
        }
        else
        {
            animator.SetTrigger("LedgeClimb1");
        }
        animator.SetBool("CanJump", false);
    }
    private void EndAnim_LedgeClimb()
    {
        animator.SetBool("CanJump", true);
    }
    private void PlayAnim_Slide()
    {
        animator.SetBool("Sliding", true);
    }
    private void EndAnim_Slide()
    {
        animator.SetBool("Sliding", false);
    }
    private void PlayAnim_Crouch()
    {
        animator.SetBool("Crouching", true);
    }
    private void EndAnim_Crouch()
    {
        animator.SetBool("Crouching", false);
    }
    private void PlayAnim_WallRun()
    {
        animator.SetBool("WallRunning", true);
    }
    private void EndAnim_Wallrun()
    {
        animator.SetBool("WallRunning", false);
    }
    private void PlayAnim_Dash(Vector3 dashInput)
    {

    }

    private void EquipUnequip()
    {
        if (!playerInput.GetIsEquiping())
        {
            //m_AnimationParams.IsEquiping = true;
            animator.SetBool("Equiping", true);
            animator.SetTrigger("Equip");
        }
        else if (playerInput.GetIsEquiping())
        {
            //m_AnimationParams.IsEquiping = false;
            animator.SetBool("Equiping", false);
        }
    }

    public void PlayAnim_Attack1()
    {
        animator.SetTrigger("Attack1");
    }
    public void PlayAnim_Attack2()
    {

    }

    IEnumerator LandCoroutine()
    {
        Transform tf = transform;
        Vector3 scale = tf.localScale;

        float ysize = 0.75f;

        float downDur = 0.025f;
        float upDur = 0.05f;
        float time = 0f;
        float cur = 0f;
        
        while (time < downDur)
        {
            cur = Mathf.Lerp(1f, ysize, time / downDur);
            tf.localScale = new Vector3(1f, cur, 1f);
            time += Time.deltaTime;
            yield return null;
        }
        yield return time = 0f;
        
        while (time < upDur)
        {
            cur = Mathf.Lerp(ysize, 1f, time / upDur);
            tf.localScale = new Vector3(1f, cur, 1f);
            time += Time.deltaTime;
            yield return null;
        }
        yield return tf.localScale = scale;
    }
    void OnAnimatorIK()
    {
        if (animator)
        {
            // Weight 업데이트
            if (saya_IkActive && !leftGrip_IKActive) // 칼집 상태
            {
                currentIKWeight = Mathf.MoveTowards(currentIKWeight, 1.0f, Time.deltaTime * ikTransitionSpeed);
                currentLeftGripIKWeight = Mathf.MoveTowards(currentLeftGripIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
            }
            else if (leftGrip_IKActive && !saya_IkActive) // 그립 상태
            {
                currentIKWeight = Mathf.MoveTowards(currentIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
                currentLeftGripIKWeight = Mathf.MoveTowards(currentLeftGripIKWeight, 1.0f, Time.deltaTime * ikTransitionSpeed);
            }
            else // 둘 다 비활성 상태
            {
                currentIKWeight = Mathf.MoveTowards(currentIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
                currentLeftGripIKWeight = Mathf.MoveTowards(currentLeftGripIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
            }
            if (m_AnimationParams.activeIK)
            {
                currentLeftHandIKWeight = Mathf.MoveTowards(currentLeftHandIKWeight, 0.9f, Time.deltaTime * ikTransitionSpeed);
            }
            else
            {
                currentLeftHandIKWeight = Mathf.MoveTowards(currentLeftHandIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
            }

            // 왼손 IK 처리
            if (leftHandObj != null && saya_IkActive) // 칼집 잡는 상태
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentIKWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
            }
            else if (leftHandGripPoint != null && leftGrip_IKActive) // 그립 잡는 상태
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentLeftGripIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentLeftGripIKWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripPoint.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGripPoint.rotation);
            }

            // Left Elbow Hint 처리 (공통)
            if (leftElbowHint != null)
            {
                float elbowWeight = Mathf.Max(currentIKWeight, currentLeftGripIKWeight) * 0.9f; // 더 큰 Weight 사용
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, elbowWeight);
                animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowHint.position);
            }/*
            if (isLookAt)
            {
                animator.SetLookAtWeight(0.75f); // 시선 강도 설정
                animator.SetLookAtPosition(targetLookAt.position); // 바라볼 위치 설정
            }*/
            
        }
    }
    public void Equip()
    {
        katana.EquipToHand();
    }
    public void UnEquip()
    {
        katana.Sheathe();
    }
    public void Equip_Reverse()
    {
        katana.EquipToHand_Reverse();
    }
}
