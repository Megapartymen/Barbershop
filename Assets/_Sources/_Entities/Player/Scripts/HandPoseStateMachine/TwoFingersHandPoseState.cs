using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoFingersHandPoseState : HandPoseState
{
    //Some variables

    public TwoFingersHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_twoFingersPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.TwoFingersHandPose;
        // Debug.Log("[HAND POSER] TWO FINGERS STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // Debug.Log("[HAND POSER] TWO FINGERS STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
