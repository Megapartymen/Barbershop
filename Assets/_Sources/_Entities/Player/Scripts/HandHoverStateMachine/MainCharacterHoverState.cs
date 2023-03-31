using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCharacterHoverState : HandHoverState
{
    //Some variables

    public MainCharacterHoverState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        _handPoser.CurrentIdlePoseState = _handPoser.TwoFingersHandPoseState;
        _handPoser.CurrentGripPoseState = _handPoser.TwoFingersHandPoseState;
        _handPoser.CurrentTriggerPoseState = _handPoser.TwoFingersPressHandPoseState;
        _handPoser.HandHoverState = HandHoverStateEnum.MainCharacterHover;
        _handPoser.OnMainCharacterHover?.Invoke(_handPoser.NearestInteractableObject.GetComponent<Character>());
        // Debug.Log("[HAND HOVER] MAIN CHARACTER HOVER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        _handPoser.OnMainCharacterDeHover?.Invoke();
        // Debug.Log("[HAND HOVER] MAIN CHARACTER HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
