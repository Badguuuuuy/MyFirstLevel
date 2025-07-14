using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModelEventHandler : MonoBehaviour
{
    public GameObject player;

    PlayerAnimationController animationController;
    PlayerAttackController attackController;

    public event Action<int> returnTypeOfAnim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animationController = GetComponent<PlayerAnimationController>();
        attackController = player.GetComponent<PlayerAttackController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnableCanCombo()
    {
        attackController.canCombo = true;
    }
    public void DisableCanCombo()
    {
        attackController.canCombo = false;
    }
    public void ReturnTypeOfAnim(int type)
    {
        returnTypeOfAnim?.Invoke(type);
    }
}
