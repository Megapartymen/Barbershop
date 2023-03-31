using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabHoverHandPoseState : HandPoseState
{
    //Some variables

    public GrabHoverHandPoseState(HandPoser handPoser, Animator animator)
    {
        _handPoser = handPoser;
        _animator = animator;
    }
    
    public override void Enter()
    {
        base.Enter();
        _animator.CrossFade(_readyToGrab, _changeAnimationTime);
        _handPoser.HandPoseState = HandPoseStateEnum.PinchHoverHandPose;
        // _handPoser.NearestInteractableObject.GetComponent<XRGrabInteractable>().enabled = true;
        // Debug.Log("[HAND POSER] PINCH HOVER STATE STARTED");
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Debug.Log("[HAND POSER] PINCH HOVER STATE ENDED");
    }

    public override void Update()
    {
        base.Update();
        //Some uniq logic
    }
}
