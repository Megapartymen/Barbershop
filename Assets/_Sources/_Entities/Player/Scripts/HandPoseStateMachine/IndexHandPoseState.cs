using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexHandPoseState : HandPoseState
{
    //Some variables

    public IndexHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_indexPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.IndexHandPose;
        // Debug.Log("[HAND POSER] INDEX STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Debug.Log("[HAND POSER] INDEX STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
