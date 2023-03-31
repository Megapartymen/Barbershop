using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoFingersPressHandPoseState : HandPoseState
{
    //Some variables

    public TwoFingersPressHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_twofingersPressPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.TwoFingersPressHandPose;
        // Debug.Log("[HAND POSER] TWO FINGERS PRESS STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        // Debug.Log("[HAND POSER] TWO FINGERS PRESS STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
