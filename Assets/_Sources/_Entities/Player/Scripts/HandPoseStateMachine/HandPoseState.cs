using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HandPoseState
{
    protected HandPoser _handPoser;
    protected Animator _animator;
    protected float _changeAnimationTime = 0.1f;

    protected readonly int _idlePoseHash = Animator.StringToHash("IdlePose");
    protected readonly int _fistPoseHash = Animator.StringToHash("FistPose");
    protected readonly int _indexPoseHash = Animator.StringToHash("IndexPose");
    protected readonly int _indexPressPoseHash = Animator.StringToHash("IndexPressPose");
    protected readonly int _twoFingersPoseHash = Animator.StringToHash("TwoFingersPose");
    protected readonly int _twofingersPressPoseHash = Animator.StringToHash("TwoFingersPressPose");
    protected readonly int _pinchPoseHash = Animator.StringToHash("PinchPose");
    protected readonly int _pinchHoverPoseHash = Animator.StringToHash("PinchHoverPose");
    protected readonly int _readyToGrab = Animator.StringToHash("ReadyToGrab");

    public virtual void Enter()
    {
        
    }
    
    public virtual void Exit()
    {
        
    }
    
    public virtual void Update()
    {
        
    }
}
