using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MainCamMovController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float distance = 20f; // ĳ���Ϳ� ī�޶� ������ �Ÿ�
    public float horizontalSpeed = 200f; // ���콺 ���� ȸ�� �ӵ�
    public float verticalSpeed = 100f; // ���콺 ���� ȸ�� �ӵ�
    public float verticalMinLimit = -40f; // ���� ȸ�� ���� (�ּ�)
    public float verticalMaxLimit = 80f; // ���� ȸ�� ���� (�ִ�)

    private float horizontalAngle = 0f; // ���� ȸ�� ����
    private float verticalAngle = 20f; // ���� ȸ�� ����

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        horizontalAngle += Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime;
        verticalAngle -= Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;

        // ���� ȸ�� ������ ����
        verticalAngle = Mathf.Clamp(verticalAngle, verticalMinLimit, verticalMaxLimit);
    }

    private void LateUpdate()
    {
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
        Vector3 offset = rotation * Vector3.back * distance;

        // ī�޶� ��ġ ������Ʈ
        transform.position = target.position + offset;

        // ī�޶� �׻� ĳ���͸� �ٶ󺸵���
        transform.LookAt(target);
    }
}
