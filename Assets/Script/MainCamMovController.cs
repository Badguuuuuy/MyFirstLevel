using System;
using System.Security.Cryptography;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public class MainCamMovController : MonoBehaviour
{
    public GameObject player;
    public Transform camPos;
    public Transform freelookPoint;
    public Transform camResetPoint;
    public Transform aimPoint;

    public Vector3 offset;

    public CinemachineCamera mainCam;
    public CinemachineCamera aimCam;

    public float distance = 20f; // 캐릭터와 카메라 사이의 거리
    public float horizontalSpeed = 200f; // 마우스 수평 회전 속도
    public float verticalSpeed = 100f; // 마우스 수직 회전 속도
    public float verticalMinLimit = -40f; // 수직 회전 제한 (최소)
    public float verticalMaxLimit = 70f; // 수직 회전 제한 (최대)
    private float horizontalAngle = 0f; // 수평 회전 각도
    private float verticalAngle = 0f; // 수직 회전 각도

    private PlayerController playerController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = player.GetComponent<PlayerController>();

        playerController.OnRightClickedToAim += ToAimCamera;
        playerController.OnRightClickedToFreeLook += ToFreeLookCamera;

        camPos.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        horizontalAngle += Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime;
        verticalAngle -= Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;

        // 수직 회전 각도의 제한
        verticalAngle = Mathf.Clamp(verticalAngle, verticalMinLimit, verticalMaxLimit);

        if (Input.GetKeyDown(KeyCode.F))
        {
            //mainCam.ForceCameraPosition(new Vector3(0f, 0f, 20f), Quaternion.Euler(0f, 0f, 0f));
            camPos.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        camPos.rotation = rotation;
        Debug.Log(verticalAngle);
    }
    private void FixedUpdate()
    {
        //////Aiming
        if (playerController.GetIsAimingCache())
        {
            player.transform.rotation = Quaternion.Euler(player.transform.rotation.eulerAngles.x, camPos.rotation.eulerAngles.y, 0f);  
        }
        //////Freelook
        else
        {
            
        }
        
    }

    private void LateUpdate()
    {
        
    }

    private void ToAimCamera(object sender, EventArgs e)
    {
        mainCam.Priority = 0;
        aimCam.Priority = 1;
    }
    private void ToFreeLookCamera(object sender, EventArgs e)
    {
        mainCam.ForceCameraPosition(camResetPoint.position, Quaternion.Euler(0f, 0f, 0f)); //////aim -> freelook 시 캠 리셋 포인트로 이동
        mainCam.Priority = 1;
        aimCam.Priority = 0;
    }
}
