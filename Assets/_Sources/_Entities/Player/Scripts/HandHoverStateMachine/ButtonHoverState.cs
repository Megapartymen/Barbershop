using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonHoverState : HandHoverState
{
    //Some variables

    public ButtonHoverState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        _handPoser.CurrentIdlePoseState = _handPoser.IndexHandPoseState;
        _handPoser.CurrentGripPoseState = _handPoser.IndexHandPoseState;
        _handPoser.CurrentTriggerPoseState = _handPoser.IndexHandPoseState;
        _handPoser.HandHoverState = HandHoverStateEnum.ButtonHover;
        // Debug.Log("[HAND HOVER] BUTTON HOVER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // Debug.Log("[HAND HOVER] BUTTON HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
