using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScissorsCutHandPoseState : HandPoseState
{
    //Some variables

    public ScissorsCutHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_scissorsCutPoseHash, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.ScissorsCutHandPose;
        
        if (_handPoser.ObjectInHand.TryGetComponent(out Scissors scissors))
        {
            scissors.SetScissorsClosed();
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
