using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaderController : MonoBehaviour
{
    [SerializeField] private Fader _fader;

    private void Start()
    {
        _fader.FadeOut();
    }
}
