using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using DG.Tweening;

public class Scissors : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _blade;

    [Space] [Header("Sounds")]
    [SerializeField] private AudioSource _scissorsClose;
    [SerializeField] private AudioSource _scissorsOpen;
    [SerializeField] private AudioSource _scissorsDroped;

    private readonly int _openScissors = Animator.StringToHash("OpenScissors");
    private readonly int _closeScissors = Animator.StringToHash("CloseScissors");

    private void Awake()
    {
        _blade.gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        _scissorsDroped.Play();
    }

    public void SetScissorsOpen()
    {
        _animator.CrossFade(_openScissors, 0.1f);
        _scissorsOpen.Play();
    }

    public void SetScissorsClosed()
    {
        _animator.CrossFade(_closeScissors, 0.1f);
        _scissorsClose.Play();
        Cut();
    }

    private void Cut()
    {
        var sequence = DOTween.Sequence();
        sequence.AppendCallback(()=> _blade.gameObject.SetActive(true))
            .Append(_blade.DOLocalMove(new Vector3(0,0,0.03f), 0.3f))
            .AppendCallback(()=> _blade.gameObject.SetActive(false))
            .AppendCallback(()=> _blade.localPosition = new Vector3(0,0,-0.03f));
    }
}
