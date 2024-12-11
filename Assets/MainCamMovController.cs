using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MainCamMovController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float distance = 20f; // 캐릭터와 카메라 사이의 거리
    public float horizontalSpeed = 200f; // 마우스 수평 회전 속도
    public float verticalSpeed = 100f; // 마우스 수직 회전 속도
    public float verticalMinLimit = -40f; // 수직 회전 제한 (최소)
    public float verticalMaxLimit = 80f; // 수직 회전 제한 (최대)

    private float horizontalAngle = 0f; // 수평 회전 각도
    private float verticalAngle = 20f; // 수직 회전 각도

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        horizontalAngle += Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime;
        verticalAngle -= Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;

        // 수직 회전 각도의 제한
        verticalAngle = Mathf.Clamp(verticalAngle, verticalMinLimit, verticalMaxLimit);
    }

    private void LateUpdate()
    {
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
        Vector3 offset = rotation * Vector3.back * distance;

        // 카메라 위치 업데이트
        transform.position = target.position + offset;

        // 카메라가 항상 캐릭터를 바라보도록
        transform.LookAt(target);
    }
}
