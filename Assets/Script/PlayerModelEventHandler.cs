using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerModelEventHandler : MonoBehaviour
{
    public GameObject player;
    public PlayerAttackEffectController playerAttackEffectController;
    [HideInInspector] public CinemachineCamera mainCam;
    [HideInInspector] public CinemachineCamera uiCam;
    PlayerAnimationController animationController;
    PlayerAttackController attackController;

    //public event Action<int> TriggerAttackVFX;

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
}
