using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchHandPoseState : HandPoseState
{
    //Some variables

    public PinchHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_pinchPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.PinchHandPose;
        // Debug.Log("[HAND POSER] PINCH STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Debug.Log("[HAND POSER] PINCH STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
