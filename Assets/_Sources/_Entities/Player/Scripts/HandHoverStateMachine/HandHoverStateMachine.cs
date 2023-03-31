using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandHoverStateMachine
{
    public HandHoverState CurrentHandPoseState { get; set; }
    
    public void Initialize(HandHoverState startHandHoverState)
    {
        CurrentHandPoseState = startHandHoverState;
        CurrentHandPoseState.Enter();
    }

    public void ChangeState(HandHoverState handHoverState)
    {
        CurrentHandPoseState.Exit();
        CurrentHandPoseState = handHoverState;
        CurrentHandPoseState.Enter();
    }
}
