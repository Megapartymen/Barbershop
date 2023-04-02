using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WalkCrunch : MonoBehaviour
{
    [SerializeField] private AudioSource _crunch;

    private VRInputSystem _inputSystem;

    private bool _isPlay;

    private void Awake()
    {
        _inputSystem = FindObjectOfType<VRInputSystem>();
    }

    private void Update()
    {
        if (_inputSystem.LeftJoystick != Vector2.zero && !_isPlay)
        {
            PlayWalkSound();
            _isPlay = true;
        }
        
        if (_inputSystem.LeftJoystick == Vector2.zero && _isPlay)
        {
            StopWalkingSound();
            _isPlay = false;
        }
    }

    private void PlayWalkSound()
    {
        _crunch.Play();
    }

    private void StopWalkingSound()
    {
        _crunch.Pause();
    }
}
