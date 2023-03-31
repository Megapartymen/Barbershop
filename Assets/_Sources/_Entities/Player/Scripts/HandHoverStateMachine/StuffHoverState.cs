using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StuffHoverState : HandHoverState
{
    private Stuff _stuff;

    public StuffHoverState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        _handPoser.CurrentIdlePoseState = _handPoser.GrabHoverHandPoseState;
        _handPoser.CurrentGripPoseState = _handPoser.PinchHandPoseState;
        _handPoser.CurrentTriggerPoseState = _handPoser.GrabHoverHandPoseState;
        _handPoser.HandHoverState = HandHoverStateEnum.StuffHover;
        _handPoser.NearestStuff = _handPoser.NearestInteractableObject;
        _stuff = _handPoser.NearestInteractableObject.GetComponent<Stuff>();
        // _handPoser.DirectInteractor.enabled = true;
        // Debug.Log("[HAND HOVER] STUFF HOVER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        _handPoser.NearestStuff = null;
        // _handPoser.DirectInteractor.enabled = false;
        // Debug.Log("[HAND HOVER] STUFF HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
