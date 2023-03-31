using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabStateMachine
{
    public GrabState CurrentGrabState { get; set; }
    
    public void Initialize(GrabState startGrabState)
    {
        CurrentGrabState = startGrabState;
        CurrentGrabState.Enter();
    }

    public void ChangeState(GrabState grabState)
    {
        CurrentGrabState.Exit();
        CurrentGrabState = grabState;
        CurrentGrabState.Enter();
    }
}
