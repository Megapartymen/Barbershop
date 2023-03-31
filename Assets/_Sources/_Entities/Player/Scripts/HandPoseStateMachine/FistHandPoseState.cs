using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistHandPoseState : HandPoseState
{
    //Some variables

    public FistHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_fistPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.FistHandPose;
        // Debug.Log("[HAND POSER] FIST STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Debug.Log("[HAND POSER] FIST STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
