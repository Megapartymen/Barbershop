using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Resizer : MonoBehaviour
{
    [SerializeField] private GameObject _playerGround;
    
    private VRInputSystem _inputSystem;
    private MainCharacterController _mainCharacterController;
    private Vector3 _bigSize;
    private Vector3 _smallSize;
    private Vector3 _bigPosition;
    private Vector3 _smallPosition;
    private bool _isBig = true;

    private void Awake()
    {
        _playerGround.SetActive(false);
        _inputSystem = FindObjectOfType<VRInputSystem>();
        _mainCharacterController = FindObjectOfType<MainCharacterController>();
        _bigSize = transform.localScale;
        _smallSize = Vector3.one;
        _bigPosition = new Vector3(0, 0, -50);
        // _smallPosition = new Vector3(12, 8.5f, -21);
        _isBig = true;
    }

    private void OnEnable()
    {
        _inputSystem.OnLeftPrimaryPressed += SetSizeSmall;
        _inputSystem.OnLeftSecondaryPressed += SetSizeBig;
    }
    
    private void OnDisable()
    {
        _inputSystem.OnLeftPrimaryPressed -= SetSizeSmall;
        _inputSystem.OnLeftSecondaryPressed -= SetSizeBig;
    }

    private void SetSizeSmall()
    {
        if (!_isBig || _mainCharacterController.FirstSelectedCharacter == null)
            return;

        var smallPosition = _mainCharacterController.FirstSelectedCharacter.transform.position;
        
        _isBig = false;
        _playerGround.SetActive(true);
        _bigPosition = transform.position;
        transform.DOScale(_smallSize, 1);
        transform.DOMove(smallPosition, 1);
        // transform.localScale = _smallSize;
        // transform.position = _smallPosition;
    }

    private void SetSizeBig()
    {
        if (_isBig)
            return;

        _isBig = true;
        _playerGround.SetActive(false);
        transform.DOScale(_bigSize, 1);
        transform.DOMove(_bigPosition, 1);
    }
}
