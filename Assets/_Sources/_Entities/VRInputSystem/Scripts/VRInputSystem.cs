using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

public enum VRController
{
    Left,
    Right
}

public enum ControllerButton
{
    Trigger,
    Grip
}

public class VRInputSystem : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActionAsset;
    
    [Space] [Header("Current rig controllers")]
    public ActionBasedController LeftController;
    public ActionBasedController RightController;

    private List<ActionBasedController> _controllers = new List<ActionBasedController>();

    private InputActionMap _leftControllerMap;
    private InputActionMap _leftLocomotionMap;
    private InputActionMap _rightControllerMap;
    private InputActionMap _rightLocomotionMap;
    private InputActionMap _extendedButtonsMap;
    
    public InputAction ButtonLeftTrigger { get; private set; }
    public InputAction ButtonLeftGrip { get; private set; }
    public InputAction ButtonLeftMenu { get; private set; }
    public InputAction ButtonLeftPrimary { get; private set; }
    public InputAction ButtonLeftSecondary { get; private set; }
    public InputAction LeftJoystickAction { get; private set; }
    
    
    public InputAction ButtonRightTrigger { get; private set; }
    public InputAction ButtonRightGrip { get; private set; }
    public InputAction ButtonRightPrimary { get; private set; }
    public InputAction ButtonRightSecondary { get; private set; }
    public InputAction RightJoystickAction { get; private set; }

    //Left controller
    [HideInInspector] public float CurrentLeftTriggerValue;
    [HideInInspector] public bool IsLeftTriggerPressed;
    [HideInInspector] public float CurrentLeftGripValue;
    [HideInInspector] public bool IsLeftGripPressed;
    [HideInInspector] public float CurrentLeftMenuValue;
    [HideInInspector] public bool IsLeftMenuPressed;
    [HideInInspector] public float CurrentLeftPrimaryValue;
    [HideInInspector] public bool IsLeftPrimaryPressed;
    [HideInInspector] public float CurrentLeftSecondaryValue;
    [HideInInspector] public bool IsLeftSecondaryPressed;

    [HideInInspector] public Vector2 LeftJoystick;

    public Action OnLeftTriggerPressed;
    public Action OnLeftTriggerUnpressed;
    public Action OnLeftGripPressed;
    public Action OnLeftGripUnpressed;
    public Action OnLeftMenuPressed;
    public Action OnLeftMenuUnpressed;
    public Action OnLeftPrimaryPressed;
    public Action OnLeftPrimaryUnpressed;
    public Action OnLeftSecondaryPressed;
    public Action OnLeftSecondaryUnpressed;

    //Right controller
    [HideInInspector] public float CurrentRightTriggerValue;
    [HideInInspector] public bool IsRightTriggerPressed;
    [HideInInspector] public float CurrentRightGripValue;
    [HideInInspector] public bool IsRightGripPressed;
    [HideInInspector] public float CurrentRightPrimaryValue;
    [HideInInspector] public bool IsRightPrimaryPressed;
    [HideInInspector] public float CurrentRightSecondaryValue;
    [HideInInspector] public bool IsRightSecondaryPressed;
    
    [HideInInspector] public Vector2 RightJoystick;
    
    public Action OnRightTriggerPressed;
    public Action OnRightTriggerUnpressed;
    public Action OnRightGripPressed;
    public Action OnRightGripUnpressed;
    public Action OnRightPrimaryPressed;
    public Action OnRightPrimaryUnpressed;
    public Action OnRightSecondaryPressed;
    public Action OnRightSecondaryUnpressed;
    
    //Both controller
    public Action OnAnyButtonPressed;
    public Action OnAnyButtonUnpressed;

    private void Awake()
    {
        FindControllers();
        SetButtonsLinks();
    }

    private void Update()
    {
        UpdateAndSendButtonsStates();
    }

    #region PreparingAndUpdateSystem

    private void SetButtonsLinks()
    {
        _leftControllerMap = _inputActionAsset.FindActionMap("XRI LeftHand Interaction");
        _leftLocomotionMap = _inputActionAsset.FindActionMap("XRI LeftHand Locomotion");
        _rightControllerMap = _inputActionAsset.FindActionMap("XRI RightHand Interaction");
        _rightLocomotionMap = _inputActionAsset.FindActionMap("XRI RightHand Locomotion");
        _extendedButtonsMap = _inputActionAsset.FindActionMap("XRI Extended Buttons");
        
        ButtonLeftTrigger = _leftControllerMap.FindAction("Activate");
        ButtonLeftGrip = _leftControllerMap.FindAction("Select Value");
        ButtonLeftMenu = _extendedButtonsMap.FindAction("Left Menu Button");
        ButtonLeftPrimary = _extendedButtonsMap.FindAction("Left Primary Button");
        ButtonLeftSecondary = _extendedButtonsMap.FindAction("Left Secondary Button");
        LeftJoystickAction = _extendedButtonsMap.FindAction("Left Joystick");
        
        ButtonRightTrigger = _rightControllerMap.FindAction("Activate");
        ButtonRightGrip = _rightControllerMap.FindAction("Select Value");
        ButtonRightPrimary = _extendedButtonsMap.FindAction("Right Primary Button");
        ButtonRightSecondary = _extendedButtonsMap.FindAction("Right Secondary Button");
        RightJoystickAction = _extendedButtonsMap.FindAction("Right Joystick");
    }

    private void FindControllers()
    {
        var controllers = FindObjectsOfType<ActionBasedController>();

        for (int i = 0; i < controllers.Length; i++)
        {
            _controllers.Add(controllers[i]);
        }
        
        // Debug.Log("[VR INPUT SYSTEM] Find " + _controllers.Count + " controller");
    }

    private void UpdateAndSendButtonsStates()
    {
        SendButtonState(ButtonLeftTrigger.ReadValue<float>(),
            ref CurrentLeftTriggerValue, OnLeftTriggerPressed, OnLeftTriggerUnpressed, ref IsLeftTriggerPressed);
        SendButtonState(ButtonRightTrigger.ReadValue<float>(),
            ref CurrentRightTriggerValue, OnRightTriggerPressed, OnRightTriggerUnpressed, ref IsRightTriggerPressed);
        SendButtonState(ButtonLeftGrip.ReadValue<float>(),
            ref CurrentLeftGripValue, OnLeftGripPressed, OnLeftGripUnpressed, ref IsLeftGripPressed);
        SendButtonState(ButtonRightGrip.ReadValue<float>(),
            ref CurrentRightGripValue, OnRightGripPressed, OnRightGripUnpressed, ref IsRightGripPressed);
        SendButtonState(ButtonLeftMenu.ReadValue<float>(),
            ref CurrentLeftMenuValue, OnLeftMenuPressed, OnLeftMenuUnpressed, ref IsLeftMenuPressed);
        SendButtonState(ButtonLeftPrimary.ReadValue<float>(),
            ref CurrentLeftPrimaryValue, OnLeftPrimaryPressed, OnLeftPrimaryUnpressed, ref IsLeftPrimaryPressed);
        SendButtonState(ButtonRightPrimary.ReadValue<float>(),
            ref CurrentRightPrimaryValue, OnRightPrimaryPressed, OnRightPrimaryUnpressed, ref IsRightPrimaryPressed);
        SendButtonState(ButtonLeftSecondary.ReadValue<float>(),
            ref CurrentLeftSecondaryValue, OnLeftSecondaryPressed, OnLeftSecondaryUnpressed, ref IsLeftSecondaryPressed);
        SendButtonState(ButtonRightSecondary.ReadValue<float>(),
            ref CurrentRightSecondaryValue, OnRightSecondaryPressed, OnRightSecondaryUnpressed, ref IsRightSecondaryPressed);

        LeftJoystick = LeftJoystickAction.ReadValue<Vector2>();
        RightJoystick = RightJoystickAction.ReadValue<Vector2>();
    }
    
    private void SendButtonState(float targetButtonValue, ref float currentButtonValue, Action onPressed, Action onUnpressed, ref bool isPressed)
    {
        if (targetButtonValue == currentButtonValue)
            return;

        if (currentButtonValue < 0.5 && targetButtonValue >= 0.5 && !isPressed)
        {
            onPressed?.Invoke();
            OnAnyButtonPressed?.Invoke();
            isPressed = true;
            // Debug.Log("PRESSED");
        }
        else if (currentButtonValue >= 0.5 && targetButtonValue < 0.5 && isPressed)
        {
            onUnpressed?.Invoke();
            OnAnyButtonUnpressed?.Invoke();
            isPressed = false;
            // Debug.Log("UNPRESSED");
        }

        currentButtonValue = targetButtonValue;
    }

    #endregion

    #region Callbacks

    public bool IsPressed(ActionBasedController controller, ControllerButton button)
    {
        if (!SetControllerFromVariable(controller))
            return false;

        switch (button)
        {
            case ControllerButton.Trigger:
                return controller.activateAction.action.IsPressed();
            case ControllerButton.Grip:
                return controller.selectAction.action.IsPressed();
            default:
                return false;
        }
    }

    #endregion
    
    #region Haptic

    public bool SendHapticImpulse(float amplitude, float duration, ActionBasedController controller)
    {
        if (!SetControllerFromVariable(controller))
            return false;

        return PlayHapticImpulse(amplitude, duration, controller);
    }
    
    public bool SendHapticImpulse(float amplitude, float duration, VRController enumController)
    {
        ActionBasedController controller;
        
        if (SetControllerFromEnum(enumController) != null)
            controller = SetControllerFromEnum(enumController);
        else
            return false;
        
        return PlayHapticImpulse(amplitude, duration, controller);
    }

    private bool PlayHapticImpulse(float amplitude, float duration, ActionBasedController controller)
    {
#if ENABLE_VR || (UNITY_GAMECORE && INPUT_SYSTEM_1_4_OR_NEWER)
        if (controller.hapticDeviceAction.action?.activeControl?.device is XRControllerWithRumble rumbleController)
        {
            rumbleController.SendImpulse(amplitude, duration);
            return true;
        }
#endif

        return false;
    }

    #endregion

    private bool SetControllerFromVariable(ActionBasedController controller)
    {
        if (controller == null)
            return false;
        
        for (int i = 0; i < _controllers.Count; i++)
        {
            if (controller == _controllers[i])
                return true;
        }

        return false;
    }
    
    private ActionBasedController SetControllerFromEnum(VRController controller)
    {
        switch (controller)
        {
            case VRController.Left:
                return LeftController;
            case VRController.Right:
                return RightController;
            default:
                return null;
        }
    }
}
