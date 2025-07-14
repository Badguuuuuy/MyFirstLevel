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
        Debug.Log("감지됨");
    }
    private void OnTriggerStay(Collider other)
    {
        //isGrounded = true;
        //Debug.Log("크하하학");
    }
    private void OnTriggerExit(Collider other)
    {
        groundContactCount = Mathf.Max(0, groundContactCount - 1);
    }
}
