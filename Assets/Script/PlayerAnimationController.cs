using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.HID;

public class PlayerAnimationController : MonoBehaviour
{
    public GameObject player;

    public Animator animator;               // �ִϸ�����   

    PlayerController playerController;
    CharacterController charController; // CharacterController
    Rigidbody rb;
    PlayerInputController playerInput;
    PlayerMovementController playerMovementController;

    public KatanaController katana;
    public Transform camPos;

    public bool leftGrip_IKActive = false;
    public bool saya_IkActive = false;
    public bool isEquipingNow = false;          ///Į�� ���� �ִ� �ִϸ��̼��� �������ΰ�?
    public bool isLookAt = false;

    private IEnumerator turnCoroutine;
    private float turnTime = 0.5f;

    public Transform upperbody;

    public Transform leftHandObj = null;
    public Transform leftHandGripPoint = null;
    public Transform leftHandGripPoint_Equip = null;
    public Transform leftElbowHint = null;

    public Transform lookPoint;

    public Vector3 initialHipRotationEuler;  // �ν����Ϳ��� �ǽð����� ���� ������ �ʱ� Hip ȸ���� (Euler Angles)
    private Quaternion initialHipRotation;  // �ʱ� Hip ȸ���� (Quaternion���� ��ȯ�Ͽ� ����)

    public Vector3 fixedSpineRotation = new Vector3(3.514341f, 56.0466f, 10.38653f);

    private float _verticalViewAngle;
    private float _horizontalViewAngle;

    private bool rotateSpine = false;

    private Quaternion hipOriginalYawRotation = Quaternion.Euler(0f, 0f, 0f);
    public float maxYawAngle = 40f;  // �ִ� (40��)
    public float rotationSmoothSpeed = 5f; // �ε巯�� ȸ�� �ӵ� ����

    float minValue = -40f;
    float maxValue = 40f;               //�¿� �밢�� ������ hip�� Ʋ������ y�� ȸ�� ������ ���� ����

    bool isStrip = false;

    private int cnt = 0;                                                                                                    ///////////���� ����, ������ ���� �� ���� ȸ�� �����Ͽ� ������ ���� �ֽ��ϵ��� ����� 
                                                                                                                            ///////////������ �޼� Į �����̿� ���� �� �¿����� �� ���� �ذ�
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

    [SerializeField] private float ikTransitionSpeed = 5.0f; // IK ��ȯ �ӵ� (���� ����)
    private float currentIKWeight = 0.0f; // ���� IK Weight ��
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

        //spineBone = animator.GetBoneTransform(HumanBodyBones.Spine); // ô�� �� ��������
        //hipBone = animator.GetBoneTransform(HumanBodyBones.Hips);
    }

    private void Update()
    {
        AnimatorStateInfo stateInfo0 = animator.GetCurrentAnimatorStateInfo(0); // 0�� ���̾� �ε���
        AnimatorStateInfo stateInfo1 = animator.GetCurrentAnimatorStateInfo(1); // 1�� ���̾� �ε���
        
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

        if (stateInfo0.IsName("Idle") || stateInfo0.IsName("Walk")) //|| (stateInfo0.IsName("Equip_Walk") && playerController.MoveZ.Value > 0f)) ///Idle����, walk����, walk_Equip�ε� playerController.MoveZ.Value > 0�϶��� true
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
            transform.rotation = Quaternion.LookRotation(new Vector3(rb.linearVelocity.normalized.x, 0f, rb.linearVelocity.normalized.z)); //�̵����� ����
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, camPos.eulerAngles.y, 0f);
        }
        //}

        //transform.rotation = Quaternion.Euler(0f, camPos.eulerAngles.y, 0f);

        //if ((playerInput.MoveX.Value != 0f) || (playerInput.MoveZ.Value != 0f)) //////wasd �Է��� �ϳ��� ������

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

        _verticalViewAngle = Vector3.SignedAngle(transform.up, camPos.up, camPos.right); ///campos�� transform�� ���� ���� ����

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

        var angle = ang * MainBone.spine.rotation;// * Quaternion.Inverse(transform.rotation); ////�÷��̾� �� �������� ȸ���� ���� �ʿ�
        

        ////�׼� ����, ���� �ִϸ��̼�, ���� �� �ִϸ��̼� �� ���� ���ǿ� ���� ó�� ��� �ٸ��� ����

        //if (playerController.CurrentState != playerController.actionState)
        //{
            if(rotSpine) //���� �� ���� �ش� ���� ȸ�� �� �ִϸ��̼ǿ� ���� ȸ�� ���� ����
            {
                MainBone.spine.rotation = Quaternion.Slerp(MainBone.spine.rotation, finalRotation_Action, Time.deltaTime * 350f);
            }
            else
            {
                if(playerMovementController.m_fsm.currentState is PlayerMovementController.ClimbState || playerMovementController.m_fsm.currentState is PlayerMovementController.WallRunState || playerMovementController.m_fsm.currentState is PlayerMovementController.AccelSlideState || playerMovementController.m_fsm.currentState is PlayerMovementController.SlideState)
                {
                    //MainBone.spine.rotation = finalRotation_Action;
                    //�ƹ��͵� �� ��
                }
                else
                {
                    MainBone.spine.rotation = finalVerticalRotation;
                }
            }
        //}

        //Debug.Log(playerMovementController.m_isWallRunning);
        //Debug.Log("����ȸ��: " + horizontalRotate.eulerAngles + " ô��: " + MainBone.spine.rotation.eulerAngles + " ķ����: " + camPos.eulerAngles);
        /*
        if (true)//playerController.CurrentState == playerController.wallRunState)
        {
            MainBone.spine.rotation = camPos.rotation * MainBone.spine.rotation * Quaternion.Euler(0f, 35.4f, 0f);//angle;
            Debug.Log("camPos" + camPos.rotation.eulerAngles.y + " ���κ� y: " + MainBone.spine.rotation.eulerAngles.y + " ��: " + camPos.rotation * MainBone.spine.rotation);
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
    private void PlayAnim_LedgeClimb(int type) ///type = 0: �Ӹ�����, 1: ��������
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
            // Weight ������Ʈ
            if (saya_IkActive && !leftGrip_IKActive) // Į�� ����
            {
                currentIKWeight = Mathf.MoveTowards(currentIKWeight, 1.0f, Time.deltaTime * ikTransitionSpeed);
                currentLeftGripIKWeight = Mathf.MoveTowards(currentLeftGripIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
            }
            else if (leftGrip_IKActive && !saya_IkActive) // �׸� ����
            {
                currentIKWeight = Mathf.MoveTowards(currentIKWeight, 0.0f, Time.deltaTime * ikTransitionSpeed);
                currentLeftGripIKWeight = Mathf.MoveTowards(currentLeftGripIKWeight, 1.0f, Time.deltaTime * ikTransitionSpeed);
            }
            else // �� �� ��Ȱ�� ����
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

            // �޼� IK ó��
            if (leftHandObj != null && saya_IkActive) // Į�� ��� ����
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentIKWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
            }
            else if (leftHandGripPoint != null && leftGrip_IKActive) // �׸� ��� ����
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentLeftGripIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentLeftGripIKWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripPoint.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGripPoint.rotation);
            }

            // Left Elbow Hint ó�� (����)
            if (leftElbowHint != null)
            {
                float elbowWeight = Mathf.Max(currentIKWeight, currentLeftGripIKWeight) * 0.9f; // �� ū Weight ���
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, elbowWeight);
                animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowHint.position);
            }/*
            if (isLookAt)
            {
                animator.SetLookAtWeight(0.75f); // �ü� ���� ����
                animator.SetLookAtPosition(targetLookAt.position); // �ٶ� ��ġ ����
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
