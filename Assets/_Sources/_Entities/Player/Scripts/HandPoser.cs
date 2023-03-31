using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

#region Enums

public enum HandPoseStateEnum
{
    IdleHandPose,
    FistHandPose,
    GrabHoverHandPose,
    IndexHandPose,
    IndexPressHandPose,
    TwoFingersHandPose,
    TwoFingersPressHandPose,
    PinchHandPose,
    PinchHoverHandPose
}

public enum HandHoverStateEnum
{
    NoHover,
    StuffHover,
    ChestHover,
    ButtonHover,
    MainCharacterHover,
    RegularCharacterHover
}

public enum GrabStateEnum
{
    SelectMainCharacter,
    DragItem,
    EmptyGrab,
    NoPressGrab
}

#endregion

public class HandPoser : MonoBehaviour
{
    //HandPoseStateMachine
    private HandPoseStateMachine _handPoseStateMachine;
    public IdleHandPoseState IdleHandPoseState;
    public FistHandPoseState FistHandPoseState;
    public IndexHandPoseState IndexHandPoseState;
    public IndexPressHandPoseState IndexPressHandPoseState;
    public TwoFingersHandPoseState TwoFingersHandPoseState;
    public TwoFingersPressHandPoseState TwoFingersPressHandPoseState;
    public PinchHandPoseState PinchHandPoseState;
    public GrabHoverHandPoseState GrabHoverHandPoseState;
    
    //HandHoverStateMachine
    private HandHoverStateMachine _handHoverStateMachine;
    private NoHoverState _noHoverState;
    private StuffHoverState _stuffHoverState;

    [Header("Variables")]
    public LayerMask HoverDetectMask;
    public Transform AttachForGrab;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _sphereCastPoint;
    
    
    [Space] [Header("Just for information")]
    public GameObject NearestInteractableObject;
    public GameObject NearestChest;
    public GameObject ObjectInHand;
    public GameObject NearestStuff;
    public HandPoseStateEnum HandPoseState;
    public HandHoverStateEnum HandHoverState;
    
    [HideInInspector] public ActionBasedController Controller;
    [HideInInspector] public HapticSystem HapticSystem;
    [HideInInspector] public HandPoseState CurrentIdlePoseState;
    [HideInInspector] public HandPoseState CurrentGripPoseState;
    [HideInInspector] public HandPoseState CurrentTriggerPoseState;
    [HideInInspector] public GrabState CurrentGrabState;
    
    private VRInputSystem _vrInputSystem;
    // private XRBaseInteractor _xrBaseInteractor;
    private bool _isDetectorActive;
    private bool _isGripPressed;
    private bool _isTriggerPressed;
    private Vector3 _basePositionSphereCastPoint;

    private void Awake()
    {
        //HandPoseStateMachine initialization
        _handPoseStateMachine = new HandPoseStateMachine();
        IdleHandPoseState = new IdleHandPoseState(this, _animator);
        FistHandPoseState = new FistHandPoseState(this, _animator);
        IndexHandPoseState = new IndexHandPoseState(this, _animator);
        IndexPressHandPoseState = new IndexPressHandPoseState(this, _animator);
        TwoFingersHandPoseState = new TwoFingersHandPoseState(this, _animator);
        TwoFingersPressHandPoseState = new TwoFingersPressHandPoseState(this, _animator);
        PinchHandPoseState = new PinchHandPoseState(this, _animator);
        GrabHoverHandPoseState = new GrabHoverHandPoseState(this, _animator);
        
        //HandHoverStateMachine initialization
        _handHoverStateMachine = new HandHoverStateMachine();
        _noHoverState = new NoHoverState(this);
        _stuffHoverState = new StuffHoverState(this);

        _vrInputSystem = FindObjectOfType<VRInputSystem>();
        // _xrBaseInteractor = GetComponent<XRBaseInteractor>();
        HapticSystem = FindObjectOfType<HapticSystem>();
        Controller = GetComponent<ActionBasedController>();
        
        // DirectInteractor = GetComponent<XRDirectInteractor>();
        // CurrentGrabState = _emptyGrabState;
    }

    private void OnEnable()
    {
        Subscribe();
    }
    
    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        _handPoseStateMachine.Initialize(GetHandPoseStateFromEnum(HandPoseState));
        _handHoverStateMachine.Initialize(GetHandHoverStateFromEnum(HandHoverState));
        _isDetectorActive = true;
        _basePositionSphereCastPoint = _sphereCastPoint.localPosition;
    }

    private void Update()
    {
        DetectHover();
        CheckPressedButtons();
        FollowSphereCastPointToStuff();
    }

    private void DetectHover()
    {
        RaycastHit[] hoverHitsInfo = Physics.SphereCastAll(_sphereCastPoint.position, 0.1f, transform.up, 0, HoverDetectMask, QueryTriggerInteraction.UseGlobal);
            
        if (hoverHitsInfo.Length > 0)
        {
            GameObject nearestObject = GetNearestInteractableObjects(hoverHitsInfo, out NearestChest);

            if (nearestObject == NearestInteractableObject)
                return;

            if (NearestInteractableObject != null)
                GetInteractionLogic(NearestInteractableObject).OnDeHovered?.Invoke();

            NearestInteractableObject = nearestObject;

            if (!_isDetectorActive)
                return;

            GetInteractionLogic(NearestInteractableObject).OnHovered?.Invoke(Controller);
            
            if (NearestInteractableObject != null)
            {
                _handHoverStateMachine.ChangeState(GetHandHoverStateFromEnum(GetInteractionLogic(NearestInteractableObject).HoverType));
            }
            else 
            { 
                _handHoverStateMachine.ChangeState(GetHandHoverStateFromEnum(HandHoverStateEnum.NoHover));
            }
                
            SetIdlePose();
        }
        else
        {
            if (NearestInteractableObject != null)
                GetInteractionLogic(NearestInteractableObject).OnDeHovered?.Invoke();
            
            NearestInteractableObject = null;
            
            if (HandHoverState != HandHoverStateEnum.NoHover && ObjectInHand == null)
            {
                _handHoverStateMachine.ChangeState(GetHandHoverStateFromEnum(HandHoverStateEnum.NoHover));
                SetIdlePose(); 
            }
        }
    }

    private GameObject GetNearestInteractableObjects(RaycastHit[] hoverHitsInfo, out GameObject nearestChest)
    {
        GameObject nearestObject = null;
        nearestChest = null;
        float nearestDistance = 0;
        
        for (int i = 0; i < hoverHitsInfo.Length; i++)
        {
            if (ObjectInHand != null && ObjectInHand == hoverHitsInfo[i].transform.gameObject)
                continue;
            
            float currentDistance = Vector3.Distance(hoverHitsInfo[i].transform.position, _sphereCastPoint.position);
            if (currentDistance < nearestDistance || nearestDistance == 0)
            {
                nearestDistance = currentDistance;
                nearestObject = hoverHitsInfo[i].transform.gameObject;
            }
        }

        return nearestObject;
    }

    private InteractionLogic GetInteractionLogic(GameObject nearestObject)
    {
        nearestObject.TryGetComponent(out InteractionLogic interactionLogic);
        return interactionLogic;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_sphereCastPoint.position, 0.1f);
        
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(_sphereCastPoint.position, 0.005f);
    }

    private void SetIdlePose()
    {
        _handPoseStateMachine.ChangeState(CurrentIdlePoseState);

        if (NearestStuff != null)
        {
            ClearObjectInHand();
        }
        
        _isDetectorActive = true;
    }
    
    private void SetGripPose()
    {
        _handPoseStateMachine.ChangeState(CurrentGripPoseState);

        if (NearestStuff != null)
        {
            SetObjectInHand();
        }
        
        _isDetectorActive = false;
    }
    
    private void SetTriggerPose()
    {
        _handPoseStateMachine.ChangeState(CurrentTriggerPoseState);
        _isDetectorActive = false;
    }

    private void SetObjectInHand()
    {
        ObjectInHand = NearestStuff;

        if (ObjectInHand != null)
        {
            var stuff = ObjectInHand.GetComponent<Stuff>();
            stuff.OnSelectEnter?.Invoke(this);
        }
    }

    private void ClearObjectInHand()
    {
        if (ObjectInHand != null)
        {
            var stuff = ObjectInHand.GetComponent<Stuff>();
        
            stuff.OnSelectExit?.Invoke();
            ObjectInHand = null;
        }
    }

    private void FollowSphereCastPointToStuff()
    {
        if (ObjectInHand == null)
        {
            _sphereCastPoint.localPosition = _basePositionSphereCastPoint;
        }
        else
        {
            _sphereCastPoint.localPosition = AttachForGrab.localPosition;
        }
    }
    
    #region Subscribe methods

    private void Subscribe()
    {
        if (Controller == _vrInputSystem.LeftController)
        {
            _vrInputSystem.OnLeftGripPressed += SetGripPose;
            _vrInputSystem.OnLeftTriggerPressed += SetTriggerPose;
            _vrInputSystem.OnLeftGripUnpressed += SetIdlePose;
            _vrInputSystem.OnLeftTriggerUnpressed += SetIdlePose;
        }
        else if (Controller == _vrInputSystem.RightController)
        {
            _vrInputSystem.OnRightGripPressed += SetGripPose;
            _vrInputSystem.OnRightTriggerPressed += SetTriggerPose;
            _vrInputSystem.OnRightGripUnpressed += SetIdlePose;
            _vrInputSystem.OnRightTriggerUnpressed += SetIdlePose;
        }
        
        // _xrBaseInteractor.onSelectEntered.AddListener(SetObjectInHand);
        // _xrBaseInteractor.onSelectExited.AddListener(ClearObjectInHand);
    }
    
    private void Unsubscribe()
    {
        if (Controller == _vrInputSystem.LeftController)
        {
            _vrInputSystem.OnLeftGripPressed -= SetGripPose;
            _vrInputSystem.OnLeftTriggerPressed -= SetTriggerPose;
            _vrInputSystem.OnLeftGripUnpressed -= SetIdlePose;
            _vrInputSystem.OnLeftTriggerUnpressed -= SetIdlePose;
        }
        else if (Controller == _vrInputSystem.RightController)
        {
            _vrInputSystem.OnRightGripPressed -= SetGripPose;
            _vrInputSystem.OnRightTriggerPressed -= SetTriggerPose;
            _vrInputSystem.OnRightGripUnpressed -= SetIdlePose;
            _vrInputSystem.OnRightTriggerUnpressed -= SetIdlePose;
        }
        
        // _xrBaseInteractor.onSelectEntered.RemoveListener(SetObjectInHand);
        // _xrBaseInteractor.onSelectExited.RemoveListener(ClearObjectInHand);
    }

    private void CheckPressedButtons()
    {
        if (Controller == _vrInputSystem.LeftController)
        {
            _isGripPressed = _vrInputSystem.IsLeftGripPressed;
            _isTriggerPressed = _vrInputSystem.IsLeftTriggerPressed;
        }
        else if (Controller == _vrInputSystem.RightController)
        {
            _isGripPressed = _vrInputSystem.IsRightGripPressed;
            _isTriggerPressed = _vrInputSystem.IsRightTriggerPressed;
        }
    }

    #endregion
    #region EnumToState

    private HandPoseState GetHandPoseStateFromEnum(HandPoseStateEnum handPoseState)
    {
        HandPoseState state = null;
        
        switch (handPoseState)
        {
            case HandPoseStateEnum.IdleHandPose :
                state = IdleHandPoseState;
                break;
            case HandPoseStateEnum.FistHandPose :
                state = FistHandPoseState;
                break;
            case HandPoseStateEnum.GrabHoverHandPose :
                state = GrabHoverHandPoseState;
                break;
            case HandPoseStateEnum.IndexHandPose :
                state = IndexHandPoseState;
                break;
            case HandPoseStateEnum.IndexPressHandPose :
                state = IndexPressHandPoseState;
                break;
            case HandPoseStateEnum.TwoFingersHandPose :
                state = TwoFingersHandPoseState;
                break;
            case HandPoseStateEnum.TwoFingersPressHandPose :
                state = TwoFingersPressHandPoseState;
                break;
            case HandPoseStateEnum.PinchHandPose :
                state = PinchHandPoseState;
                break;
            case HandPoseStateEnum.PinchHoverHandPose :
                state = GrabHoverHandPoseState;
                break;
        }

        return state;
    }
    
    private HandHoverState GetHandHoverStateFromEnum(HandHoverStateEnum handHoverState)
    {
        HandHoverState state = null;
        
        switch (handHoverState)
        {
            case HandHoverStateEnum.NoHover :
                state = _noHoverState;
                break;
            case HandHoverStateEnum.StuffHover :
                state = _stuffHoverState;
                break;
        }

        return state;
    }
    
    // private GrabState GetGrabStateFromEnum(GrabStateEnum grabState)
    // {
    //     GrabState state = null;
    //     
    //     switch (grabState)
    //     {
    //         case GrabStateEnum.SelectMainCharacter :
    //             state = _selectMainCharacterGrabState;
    //             break;
    //         case GrabStateEnum.DragItem :
    //             state = _dragItemGrabState;
    //             break;
    //         case GrabStateEnum.EmptyGrab :
    //             state = _emptyGrabState;
    //             break;
    //     }
    //
    //     return state;
    // }

    #endregion
}
