using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleHandPoseState : HandPoseState
{
    //Some variables

    public IdleHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_idlePoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.IdleHandPose;
        // Debug.Log("[HAND POSER] IDLE STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Debug.Log("[HAND POSER] IDLE STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
