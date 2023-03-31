using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMainCharacterGrabState : GrabState
{
    //Some variables

    public SelectMainCharacterGrabState(HandPoser handPoser)
    {
        _handPoser = handPoser;
    }
    
    public override void Enter()
    {
        base.Enter();
        // _handPoser.GrabState = GrabStateEnum.SelectMainCharacter;
        // _handPoser.HoverDetectMask = _handPoser.MainCharacters;
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
