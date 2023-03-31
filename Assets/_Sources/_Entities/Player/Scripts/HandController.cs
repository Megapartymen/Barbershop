using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandController : MonoBehaviour
{
    [SerializeField] private ActionBasedController _controller;
    [SerializeField] private Animator _animator;
    
    private VRInputSystem _vrInputSystem;
    private XRDirectInteractor _directInteractor;
    
    public bool IsReadyToGrab;
    public bool Grab;
    public bool IsReadyToPush;

    private void Awake()
    {
        _vrInputSystem = FindObjectOfType<VRInputSystem>();
        _directInteractor = GetComponent<XRDirectInteractor>();
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (_controller == _vrInputSystem.LeftController)
        {
            _animator.SetFloat("Grip", _vrInputSystem.CurrentLeftGripValue);
        }
        else if (_controller == _vrInputSystem.RightController)
        {
            _animator.SetFloat("Grip", _vrInputSystem.CurrentRightGripValue);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out XRGrabInteractable interactable))
        {
            IsReadyToGrab = true;
            _animator.SetBool("Hover", true);
        }

        if (other.TryGetComponent(out PushableMarker pushableMarker))
        {
            IsReadyToPush = true;
            _animator.SetBool("ReadyToPush", true);
        }

        // if (other.TryGetComponent(out Inventory pocket))
        // {
        //     Debug.Log("!!!!!!!!!!!!!!!! " + _directInteractor.interactablesSelected[0]);
        // }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out XRGrabInteractable interactable))
        {
            IsReadyToGrab = false;
            _animator.SetBool("Hover", false);
        }
        
        if (other.TryGetComponent(out PushableMarker pushableMarker))
        {
            IsReadyToPush = false;
            _animator.SetBool("ReadyToPush", false);
        }
    }
}
