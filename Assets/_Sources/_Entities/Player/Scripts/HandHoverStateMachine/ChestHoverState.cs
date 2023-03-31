using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestHoverState : HandHoverState
{
    //Some variables

    public ChestHoverState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }

    public override void Enter()
    {
        base.Enter();
        _handPoser.CurrentIdlePoseState = _handPoser.IndexHandPoseState;
        _handPoser.CurrentGripPoseState = _handPoser.IndexHandPoseState;
        _handPoser.CurrentTriggerPoseState = _handPoser.IndexPressHandPoseState;
        _handPoser.HandHoverState = HandHoverStateEnum.ChestHover;
        // Debug.Log("[HAND HOVER] CHEST HOVER STATE STARTED");
    }

    public override void Exit()
    {
        base.Exit();
        // Debug.Log("[HAND HOVER] CHEST HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
