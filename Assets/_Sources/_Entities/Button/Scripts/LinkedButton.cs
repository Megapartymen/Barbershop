using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LinkedButton : MonoBehaviour
{
    [HideInInspector] public VRButton VRButton;
    
    [SerializeField] private List<LinkedButton> _linkedButtons;

    

    private void Awake()
    {
        VRButton = GetComponent<VRButton>();
    }

    private void OnEnable()
    {
        VRButton.OnPress += UnPressLinkedButtons;
    }

    private void OnDisable()
    {
        VRButton.OnPress -= UnPressLinkedButtons;
    }

    private void UnPressLinkedButtons()
    {
        foreach (var button in _linkedButtons)
        {
            button.VRButton.IsPressed = false;
            button.VRButton.EndPress(new XRPokeInteractor());
        }
    }
}
