using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class VRButton : XRBaseInteractable
{
    // public UnityEvent OnPress = null;
    [Header("Settings")]
    [SerializeField] private Image _buttonCover;
    [SerializeField] private TextMeshProUGUI _buttonText;
    [SerializeField] private List<Transform> _pressFrames;

    // private Color _baseUiColor;
    [HideInInspector] public bool IsPressed;
    
    private float _yMin = 0;
    private float _yMax = 0;
    private bool _isPreviousPress;
    private Collider _collider;
    private float _previousHandHeight = 0;
    private XRBaseInteractor _hoverInteractor = null;
    private LinkedButton _linkedButton;
    private bool _isLinkedButton;

    public Action OnPress;
    
    protected override void Awake()
    {
        base.Awake();
        _collider = GetComponent<Collider>();
        _linkedButton = GetComponent<LinkedButton>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        onHoverEntered.AddListener(StartPress);
        onHoverExited.AddListener(EndPress);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        onHoverEntered.RemoveListener(StartPress);
        onHoverExited.RemoveListener(EndPress);
    }

    private void Start()
    {
        // _baseUiColor = _buttonCover.color;
        _isLinkedButton = _linkedButton != null;
        SetMinMax();
        // SetPressFrameOnPosition();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (_hoverInteractor)
        {
            float newHandHeight = GetLocalYPosition(_hoverInteractor.transform.position);
            float handDifference = _previousHandHeight - newHandHeight;
            _previousHandHeight = newHandHeight;

            float newPosition = transform.localPosition.y - handDifference;

            if (_isLinkedButton && IsPressed)
            {
                SetYPosition(_yMin);
            }
            else
            {
                SetYPosition(newPosition);
            }
            
            // SetPressFrameOnPosition();
            CheckPress();
        }
    }

    private void StartPress(XRBaseInteractor interactor)
    {
        _hoverInteractor = interactor;
        _previousHandHeight = GetLocalYPosition(_hoverInteractor.transform.position);
    }

    public void EndPress(XRBaseInteractor interactor)
    {
        _hoverInteractor = null;
        _previousHandHeight = 0;
        _isPreviousPress = false;

        if (_isLinkedButton && IsPressed)
        {
            SetButtonPressed();
        }
        else
        {
            SetButtonUnPressed();
        }
    }

    private void SetMinMax()
    {
        Vector3 boundsInLocal = transform.InverseTransformVector(_collider.bounds.size);
        _yMin = transform.localPosition.y - (boundsInLocal.y * 0.5f);
        _yMax = transform.localPosition.y;
    }

    private float GetLocalYPosition(Vector3 position)
    {
        Vector3 localPosition = transform.InverseTransformVector(position);
        return localPosition.y;
    }

    private void SetYPosition(float position)
    {
        Vector3 newPosition = transform.localPosition;
        newPosition.y = Mathf.Clamp(position, _yMin, _yMax);
        transform.localPosition = newPosition;
    }

    private void CheckPress()
    {
        // bool inPosition = InPosition();

        if (InPosition() /*inPosition && inPosition != _isPreviousPress*/)
        {
            if (!IsPressed)
            {
                OnPress?.Invoke();
            }
            
            IsPressed = true;
            // SetButtonColor(Color.white);
        }
        else
        {
            IsPressed = false;
            // SetButtonColor(_baseUiColor);
        }

        // _isPreviousPress = inPosition;
    }

    private bool InPosition()
    {
        float inRange = Mathf.Clamp(transform.localPosition.y, _yMin, _yMin + (_collider.bounds.size.y * 0.4f));
        return transform.localPosition.y == inRange;
    }

    private void SetButtonColor(Color color)
    {
        _buttonCover.color = color;
        _buttonText.color = color;
    }
    
    public void SetButtonPressed()
    {
        SetYPosition(_yMin);
        // SetButtonColor(Color.white);
        // SetPressFrameOnPosition();
    }

    public void SetButtonUnPressed()
    {
        SetYPosition(_yMax);
        // SetButtonColor(_baseUiColor);
        // SetPressFrameOnPosition();
    }

    private void SetPressFrameOnPosition()
    {
        _pressFrames[0].localPosition = new Vector3(_pressFrames[0].localPosition.x, _pressFrames[0].localPosition.y, _yMin - transform.localPosition.y);
        _pressFrames[1].localPosition = new Vector3(_pressFrames[1].localPosition.x, _pressFrames[1].localPosition.y, (((_yMin - transform.localPosition.y) / _pressFrames.Count) * 2) * -1);
        _pressFrames[2].localPosition = new Vector3(_pressFrames[2].localPosition.x, _pressFrames[2].localPosition.y, (((_yMin - transform.localPosition.y) / _pressFrames.Count)) * -1);
    }
}
