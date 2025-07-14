using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour, Unity.Cinemachine.IInputAxisOwner
{
    PlayerController playerController;

    PlayerInput playerInput;

    PlayerAttackController playerAttackController;

    PlayerUIHandler playerUIHandler;

    private int cnt = 0;

    private bool isAiming = false;
    private bool isAimingCache = false;
    private bool isEquiping = false;
    private bool cache = false;

    public Action OnRightClickedToAim;
    public Action OnRightClickedToFreeLook;
    public Action OnZButtonPressed;
    public Action Turn180;

    private Vector2 previousDirection = Vector2.zero;

    [Header("Input Axes")]

    [Tooltip("Horizontal Look.")]
    [HideInInspector] public InputAxis HorizontalLook = new() { Range = new Vector2(-180, 180), Wrap = true, Recentering = InputAxis.RecenteringSettings.Default };

    [Tooltip("Vertical Look.")]
    [HideInInspector] public InputAxis VerticalLook = new() { Range = new Vector2(-70, 70), Recentering = InputAxis.RecenteringSettings.Default };

    [Tooltip("X Axis movement.  Value is -1..1.  Controls the sideways movement")]
    [HideInInspector] public InputAxis MoveX = InputAxis.DefaultMomentary;

    [Tooltip("Z Axis movement.  Value is -1..1. Controls the forward movement")]
    [HideInInspector] public InputAxis MoveZ = InputAxis.DefaultMomentary;

    [Tooltip("Jump movement.  Value is 0 or 1. Controls the vertical movement")]
    [HideInInspector] public InputAxis Jump = InputAxis.DefaultMomentary;

    [Tooltip("Sprint movement.  Value is 0 or 1. If 1, then is sprinting")]
    [HideInInspector] public InputAxis Sprint = InputAxis.DefaultMomentary;

    [Tooltip("Equiping.  Value is 0 or 1. If 1, then is equiping")]
    [HideInInspector] public InputAxis Equiping = InputAxis.DefaultMomentary;

    [Tooltip("LeftMouse Attack.  Value is 0 or 1. If 1, then is attacking")]
    [HideInInspector] public InputAxis Attack = InputAxis.DefaultMomentary;

    [Tooltip("Dodge.  Value is 0 or 1. If 1, then is dodging")]
    [HideInInspector] public InputAxis Dodge = InputAxis.DefaultMomentary;

    [Tooltip("Crouch.  Value is 0 or 1. If 1, then is crouching")]
    [HideInInspector] public InputAxis Crouch = InputAxis.DefaultMomentary;


    [Header("Events")]
    [Tooltip("This event is sent when the player lands after a jump.")]
    [HideInInspector] public UnityEvent Landed = new();

    void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
    {
        axes.Add(new() { DrivenAxis = () => ref HorizontalLook, Name = "Horizontal Look", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
        axes.Add(new() { DrivenAxis = () => ref VerticalLook, Name = "Vertical Look", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
        axes.Add(new() { DrivenAxis = () => ref MoveX, Name = "Move X", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
        axes.Add(new() { DrivenAxis = () => ref MoveZ, Name = "Move Z", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
        axes.Add(new() { DrivenAxis = () => ref Jump, Name = "Jump" });
        axes.Add(new() { DrivenAxis = () => ref Sprint, Name = "Sprint" });
        axes.Add(new() { DrivenAxis = () => ref Equiping, Name = "Equip/Unequip" });
        axes.Add(new() { DrivenAxis = () => ref Attack, Name = "Attack" });
        axes.Add(new() { DrivenAxis = () => ref Dodge, Name = "Dodge" });
        axes.Add(new() { DrivenAxis = () => ref Crouch, Name = "Crouch" });
    }
    void Awake()
    {
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        playerAttackController = GetComponent<PlayerAttackController>();
        playerUIHandler = GetComponent<PlayerUIHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool GetIsAimingCache() {
        return isAimingCache;
    }

    private void EquipUnequip()
    {
        if (!isEquiping)
        {
            isEquiping = true;
        }
        else if (isEquiping)
        {
            isEquiping = false;
        }
    }

    public void ActionEquipUnequip(InputAction.CallbackContext context)
    {
        //Debug.Log("MyFunction called from: " + Environment.StackTrace);
        //OnZButtonPressed?.Invoke();
        //EquipUnequip();
    }

    
    public void ActionAttack(InputAction.CallbackContext context)
    {
        if (playerController.CurrentState != playerController.uiState)
        {
            if (context.performed)
            {
                playerAttackController.UseAttack1();
            }
        }
    }
    public void UIOpenMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerUIHandler.TogglePauseMenu();
        }
    }
    public bool GetIsEquiping()
    {
        return isEquiping;
    }
    
}
