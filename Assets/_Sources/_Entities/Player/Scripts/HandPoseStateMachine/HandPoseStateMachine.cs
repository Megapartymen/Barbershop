using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPoseStateMachine
{
    public HandPoseState CurrentHandPoseState { get; set; }
    
    public void Initialize(HandPoseState startHandPoseState)
    {
        CurrentHandPoseState = startHandPoseState;
        CurrentHandPoseState.Enter();
    }

    public void ChangeState(HandPoseState handPoseState)
    {
        CurrentHandPoseState.Exit();
        CurrentHandPoseState = handPoseState;
        CurrentHandPoseState.Enter();
    }
}
