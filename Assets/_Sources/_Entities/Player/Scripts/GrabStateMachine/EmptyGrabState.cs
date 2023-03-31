using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyGrabState : GrabState
{
    //Some variables

    public EmptyGrabState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        // _handPoser.GrabState = GrabStateEnum.EmptyGrab;
        // _handPoser.HoverDetectMask = _handPoser.NoInteractable;
        // Debug.Log("[GRAB] SELECT MAIN CHARACTER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // _handPoser.HoverDetectMask = _handPoser.AllInteractable;
        // Debug.Log("[GRAB] SELECT MAIN CHARACTER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
