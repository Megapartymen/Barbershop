using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using DG.Tweening;

public class HapticSystem : MonoBehaviour
{
    private VRInputSystem _vrInputSystem;

    private void Awake()
    {
        _vrInputSystem = FindObjectOfType<VRInputSystem>();
    }

    public void PlayExplosion(ActionBasedController controller)
    {
        
    }

    public void PlayTouch(ActionBasedController controller)
    {
        _vrInputSystem.SendHapticImpulse(0.02f, 0.02f, controller);
    }

    public void PlayApplyEffect(ActionBasedController controller)
    {
        Sequence applyEffect = DOTween.Sequence();
        applyEffect
            .AppendCallback(() => _vrInputSystem.SendHapticImpulse(0.02f, 0.02f, controller))
            .AppendInterval(0.1f)
            .AppendCallback(() => _vrInputSystem.SendHapticImpulse(0.02f, 0.02f, controller));
    }

    public void PlayShoot(ActionBasedController controller)
    {
        
    }

    public void PlayHit(ActionBasedController controller)
    {
        
    }
}
