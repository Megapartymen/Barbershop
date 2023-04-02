using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// [RequireComponent(typeof(Outline))]
public class InteractionLogic : MonoBehaviour
{
    private HapticSystem _hapticSystem;
    // private Outline _outline;
    
    [SerializeField] private LayerMask _handLayer;
    public HoverDot HoverDot;
    
    // public UIScaler UIScaler;
    public HandHoverStateEnum HoverType;
    public bool IsHovered;
    public float PrivacyRadius = 1;
    

    public Action<ActionBasedController> OnHovered;
    public Action OnDeHovered;

    private void Awake()
    {
        _hapticSystem = FindObjectOfType<HapticSystem>();
        // _outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        OnHovered += EnableHoverState;
        OnDeHovered += DisableHoverState;
    }

    private void OnDisable()
    {
        OnHovered -= EnableHoverState;
        OnDeHovered -= DisableHoverState;
    }
    
    private void Start()
    {
        SetHoverType();
        // _outline.enabled = false;
        // _outline.OutlineColor = Color.white;
        // _outline.OutlineMode = Outline.Mode.OutlineVisible;
    }
    
    private void SetHoverType()
    {
        if (TryGetComponent(out Stuff stuff))
        {
            HoverType = HandHoverStateEnum.StuffHover;
        }
    }
    
    public void EnableHoverState(ActionBasedController controller)
    {
        HoverDot.SetCircleActive();
        _hapticSystem.PlayTouch(controller);
    }

    public void DisableHoverState()
    {
        HoverDot.SetDotActive();
    }
}
