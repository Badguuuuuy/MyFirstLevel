using System.Collections;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    Animator animator; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine("Routine");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Routine()
    {
        bool abc = false;
        while (true)
        {
            animator.SetBool("switch", abc);
            abc = !abc;
            yield return new WaitForSeconds(2f);
        }
    }
}
