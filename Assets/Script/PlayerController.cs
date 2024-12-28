using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float verticalInput;
    public float horizontalInput;
    public float verticalInputRaw;
    public float horizontalInputRaw;
    public bool rightClickInput;

    private bool isAiming = false;
    private bool isAimingCache = false;
    private bool cache = false;


    public EventHandler OnRightClickedToAim;
    public EventHandler OnRightClickedToFreeLook;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //////Aiming Toggling Work
        isAiming = rightClickInput;

        if (isAiming)
        {
            isAimingCache = !isAimingCache;
        }

        //////Aiming
        if (isAimingCache)
        {
            if (!cache && isAimingCache)
            {
                OnRightClickedToAim?.Invoke(this, EventArgs.Empty);
            }
        }
        //////Freelook
        else
        {
            if (cache && !isAimingCache)
            {
                OnRightClickedToFreeLook?.Invoke(this, EventArgs.Empty);
            }
        }
        
        cache = isAimingCache; //////update rightclick toggling cache


        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInputRaw = Input.GetAxisRaw("Vertical");
        horizontalInputRaw = Input.GetAxisRaw("Horizontal");

        rightClickInput = Input.GetMouseButtonDown(1);
    }

    public bool GetIsAimingCache() {
        return isAimingCache;
    }
}
