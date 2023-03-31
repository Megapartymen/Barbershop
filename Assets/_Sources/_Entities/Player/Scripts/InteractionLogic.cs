using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Outline))]
public class InteractionLogic : MonoBehaviour
{
    private HapticSystem _hapticSystem;
    private Outline _outline;
    
    [SerializeField] private LayerMask _handLayer;
    
    public UIScaler UIScaler;
    public HandHoverStateEnum HoverType;
    public bool IsHovered;
    public float PrivacyRadius = 1;
    

    public Action<ActionBasedController> OnHovered;
    public Action OnDeHovered;

    private void Awake()
    {
        _hapticSystem = FindObjectOfType<HapticSystem>();
        _outline = GetComponent<Outline>();
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
        _outline.enabled = false;
        _outline.OutlineColor = Color.white;
        _outline.OutlineMode = Outline.Mode.OutlineVisible;
        
        if (UIScaler != null)
            HideUI();
        // UIScaler.GameObject().SetActive(false);
    }
    
    

    private void SetHoverType()
    {
        if (TryGetComponent(out Character character))
        {
            if (character.RoleCharacterState == RoleCharacterStateEnum.MainCharacter)
            {
                HoverType = HandHoverStateEnum.MainCharacterHover;
            }
            else if (character.RoleCharacterState == RoleCharacterStateEnum.RegularCharacter)
            {
                HoverType = HandHoverStateEnum.RegularCharacterHover;
            }
        }
        else if (TryGetComponent(out Stuff stuff))
        {
            HoverType = HandHoverStateEnum.StuffHover;
        }
        else if (TryGetComponent(out Chest chest))
        {
            HoverType = HandHoverStateEnum.ChestHover;
        }
        else if (TryGetComponent(out VRButton button))
        {
            HoverType = HandHoverStateEnum.ButtonHover;
        }
    }
    
    private void EnableHoverState(ActionBasedController controller)
    {
        if (UIScaler != null && UIScaler.IsShowOnHover)
            ShowUI();
        
        _outline.enabled = true;
        _hapticSystem.PlayTouch(controller);
    }

    private void DisableHoverState()
    {
        if (UIScaler != null && UIScaler.IsShowOnHover)
            HideUI();
        
        _outline.enabled = false;
    }

    public void ShowUI()
    {
        UIScaler.gameObject.SetActive(true);
        UIScaler.SetNormalSize();
    }

    public void HideUI()
    {
        var seq = DOTween.Sequence()
            .AppendCallback(() => UIScaler.SetZeroSize())
            .AppendInterval(UIScaler.ShowAnimationTime)
            .AppendCallback(() => UIScaler.gameObject.SetActive(false));
    }
}
