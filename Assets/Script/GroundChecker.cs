using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    private int groundContactCount = 0;
    [HideInInspector] public bool isGrounded => groundContactCount > 0;

    private void Update()
    {
        Debug.Log("grounded: "+ isGrounded);
    }

    private void OnTriggerEnter(Collider other)
    {
        groundContactCount++;
        Debug.Log("������");
    }
    private void OnTriggerStay(Collider other)
    {
        //isGrounded = true;
        //Debug.Log("ũ������");
    }
    private void OnTriggerExit(Collider other)
    {
        groundContactCount = Mathf.Max(0, groundContactCount - 1);
    }
}
