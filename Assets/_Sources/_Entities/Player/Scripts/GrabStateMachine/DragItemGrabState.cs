using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragItemGrabState : GrabState
{
    //Some variables

    public DragItemGrabState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        // _handPoser.GrabState = GrabStateEnum.DragItem;
        // _handPoser.HoverDetectMask = _handPoser.Chests;
        // Debug.Log("[GRAB] DRAG ITEM STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // _handPoser.HoverDetectMask = _handPoser.AllInteractable;
        // Debug.Log("[GRAB] DRAG ITEM STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
