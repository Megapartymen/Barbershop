using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HoverDot : MonoBehaviour
{
    [SerializeField] private Transform _dot;
    [SerializeField] private Transform _circle;

    public bool IsHide;

    private void Start()
    {
        SetDotActive();
    }

    public void SetDotActive()
    {
        if (IsHide)
            return;
        
        var sequence = DOTween.Sequence()
            .Append(_dot.DOScale(Vector3.one, 0.2f))
            .Join(_circle.DOScale(Vector3.zero, 0.2f));
    }

    public void SetCircleActive()
    {
        if (IsHide)
            return;
        
        var sequence = DOTween.Sequence()
            .Append(_dot.DOScale(Vector3.zero, 0.2f))
            .Join(_circle.DOScale(Vector3.one, 0.2f));
    }
    
    public void DisableDot()
    {
        var sequence = DOTween.Sequence()
            .Append(_dot.DOScale(Vector3.zero, 0.2f))
            .Join(_circle.DOScale(Vector3.zero, 0.2f));
    }
}
