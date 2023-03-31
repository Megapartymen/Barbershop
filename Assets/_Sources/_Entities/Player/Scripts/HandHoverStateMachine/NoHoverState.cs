using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoHoverState : HandHoverState
{
    //Some variables

    public NoHoverState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        _handPoser.CurrentIdlePoseState = _handPoser.IdleHandPoseState;
        _handPoser.CurrentGripPoseState = _handPoser.FistHandPoseState;
        _handPoser.CurrentTriggerPoseState = _handPoser.IdleHandPoseState;
        _handPoser.HandHoverState = HandHoverStateEnum.NoHover;
        // Debug.Log("[HAND HOVER] NO HOVER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // Debug.Log("[HAND HOVER] NO HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
