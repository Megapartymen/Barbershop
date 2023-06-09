using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using DG.Tweening;

public class Stuff : MonoBehaviour
{
    public GameObject Visible;
    
    private bool _isInSocket;
    private XRGrabInteractable _xrGrabInteractable;
    private HandPoser _handPoser;
    private Transform _handAttach;
    private float _lerpDelay = 0.5f;
    private Rigidbody _rigidbody;
    
    public InteractionLogic InteractionLogic;

    public Action<HandPoser> OnSelectEnter;
    public Action OnSelectExit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        OnSelectEnter += SetStateInHand;
        OnSelectExit += SetStateFree;
    }

    private void OnDisable()
    {
        OnSelectEnter -= SetStateInHand;
        OnSelectExit -= SetStateFree;
    }

    private void Start()
    {
        SetStateFree();
    }

    private void Update()
    {
        Follow();
    }
    
    private void Follow()
    {
        if (_handAttach != null)
        {
            transform.position = _handAttach.transform.position;
            transform.rotation = _handAttach.transform.rotation;
        }
    }

    private void SetStateFree()
    {
        _handPoser = null;
        _handAttach = null;
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
        InteractionLogic.HoverDot.IsHide = false;
        InteractionLogic.HoverDot.SetDotActive();
    }

    private void SetStateInHand(HandPoser handPoser)
    {
        if (_handPoser != null)
        {
            _handPoser.ClearObjectInHand();
            SetStateFree();
        }
        
        _handPoser = handPoser;
        _handAttach = _handPoser.AttachForGrab;
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        InteractionLogic.HoverDot.DisableDot();
        InteractionLogic.HoverDot.IsHide = true;
    }
}
