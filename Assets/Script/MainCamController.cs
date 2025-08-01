using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Cinemachine;
using Unity.Cinemachine.Samples;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public class MainCamController : MonoBehaviour
{
    public GameObject player;

    public GameObject model;

    public Transform camPos;

    public Transform recenterPoint;

    public Camera uiCam;

    private PlayerMovementController playerMovementController;
    private CinemachineThirdPersonFollow mainCamThirdPersonFollow;

    Vector3 targetPosition;

    Transform m_ControllerTransform;

    [SerializeField] private CinemachineCamera mainCamPrefab;
    [SerializeField] private CinemachineCamera uiCamPrefab;

    [HideInInspector] public CinemachineCamera mainCamInstance;
    [HideInInspector] public CinemachineCamera uiCamInstance;

    private PlayerInputController playerInput;

    private bool toggleCam = false;

    //PlayerMovementController m_Controller;

    Transform m_Controller;

    Vector3 offset = new Vector3(0.8f, 1.4f, -1f);

    public enum CouplingMode { Coupled, CoupledWhenMoving, Decoupled }

    public CouplingMode couplingMode;

    private void Awake()
    {
        playerMovementController = player.GetComponent<PlayerMovementController>();
    }

    private void OnEnable()
    {
        //m_Controller = player.GetComponent<PlayerMovementController>();
        m_Controller = model.transform;
        if (m_Controller == null)
            Debug.LogError("SimplePlayerController not found on parent object");
        else
        {
            m_ControllerTransform = m_Controller.transform;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInput = player.GetComponent<PlayerInputController>();

        camPos.rotation = Quaternion.Euler(0f, 0f, 0f);

        mainCamInstance = Instantiate(mainCamPrefab);
        uiCamInstance = Instantiate(uiCamPrefab);
        mainCamThirdPersonFollow = mainCamInstance.GetComponent<CinemachineThirdPersonFollow>();

        mainCamThirdPersonFollow.CameraSide = 1f;

        mainCamInstance.Target.TrackingTarget = camPos.transform;


        uiCamInstance.Target.TrackingTarget = camPos.transform;
    }

    // Update is called once per frame
    void Update()
    {
        //var t = camPos;
        //var yValue = Mathf.Clamp(playerController.HorizontalLook.Value, );
        //Debug.Log(playerController.VerticalLook.Value);
        
        //m_DesiredWorldRotation = t.rotation;

        //RecenterPlayer();

        //targetPosition = new Vector3(recenterPoint.position.x, player.transform.position.y, recenterPoint.position.z);

        //player.transform.LookAt(targetPosition);
    }
    private void FixedUpdate()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
            camPos.rotation = Quaternion.Euler(playerInput.VerticalLook.Value, playerInput.HorizontalLook.Value, 0);

        if (playerMovementController.m_isWallRunning && playerMovementController.isWallRight)
        {
            mainCamThirdPersonFollow.CameraSide = Mathf.Lerp(mainCamThirdPersonFollow.CameraSide, 0f, 5f * Time.deltaTime);
        }
        else
        {
            mainCamThirdPersonFollow.CameraSide = Mathf.Lerp(mainCamThirdPersonFollow.CameraSide, 1f, 5f * Time.deltaTime);
        }
    }
    public void RecenterPlayer(float damping = 0)
    {
        if (m_ControllerTransform == null)
            return;

        // Get my rotation relative to parent
        var rot = camPos.localRotation.eulerAngles;
        rot.y = NormalizeAngle(rot.y);
        var delta = rot.y;
        delta = Damper.Damp(delta, damping, Time.deltaTime);

        // Rotate the parent towards me
        m_ControllerTransform.rotation = Quaternion.AngleAxis(
            delta, m_ControllerTransform.up) * m_ControllerTransform.rotation;

        // Rotate me in the opposite direction
        playerInput.HorizontalLook.Value -= delta;
        rot.y -= delta;
        camPos.localRotation = Quaternion.Euler(rot);
    }
    float NormalizeAngle(float angle)
    {
        while (angle > 180)
            angle -= 360;
        while (angle < -180)
            angle += 360;
        return angle;
    }
    public void EnableUICamera()
    {
        mainCamInstance.Priority = 1;
        uiCamInstance.Priority = 2;
        uiCamInstance.transform.position = player.transform.position + model.transform.right * offset.x + model.transform.up * offset.y + model.transform.forward * offset.z;
        uiCamInstance.transform.rotation = Quaternion.Euler(new Vector3(0f, camPos.rotation.eulerAngles.y, 0f));
        uiCam.transform.position = player.transform.position + model.transform.right * offset.x + model.transform.up * offset.y + model.transform.forward * offset.z;
        uiCam.transform.rotation = Quaternion.Euler(new Vector3(0f, camPos.rotation.eulerAngles.y, 0f));
    }
    public void EnableMainCamera()
    {
        mainCamInstance.Priority = 2;
        uiCamInstance.Priority = 1;
    }
}
