using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegularCharacterHoverState : HandHoverState
{
    //Some variables

    public RegularCharacterHoverState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        _handPoser.CurrentIdlePoseState = _handPoser.IndexHandPoseState;
        _handPoser.CurrentGripPoseState = _handPoser.IndexHandPoseState;
        _handPoser.CurrentTriggerPoseState = _handPoser.IndexPressHandPoseState;
        _handPoser.HandHoverState = HandHoverStateEnum.RegularCharacterHover;
        // Debug.Log("[HAND HOVER] REGULAR CHARACTER HOVER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // Debug.Log("[HAND HOVER] REGULAR CHARACTER HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
