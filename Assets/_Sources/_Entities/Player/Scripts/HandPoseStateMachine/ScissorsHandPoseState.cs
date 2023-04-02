using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScissorsHandPoseState : HandPoseState
{
    //Some variables

    public ScissorsHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_scissorsPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.ScissorsHandPose;

        if (_handPoser.NearestStuff.TryGetComponent(out Scissors scissors))
        {
            scissors.SetScissorsOpen();
        }
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
