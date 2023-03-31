using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexPressHandPoseState : HandPoseState
{
    //Some variables

    public IndexPressHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_indexPressPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.IndexPressHandPose;
        // Debug.Log("[HAND POSER] INDEX PRESS STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Debug.Log("[HAND POSER] INDEX PRESS STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
