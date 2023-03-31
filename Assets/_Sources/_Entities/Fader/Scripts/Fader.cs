using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Random = UnityEngine.Random;

public class Fader : MonoBehaviour
{
    [SerializeField] private Renderer _blackScreenRenderer;
    [SerializeField] private Renderer _vignetteRenderer;
    [SerializeField] private Renderer _tunnelViewRenderer;
    // [SerializeField] private Loading _loading;
    [SerializeField] private CanvasGroup _hud;

    private Material _blackScreen;
    private Material _vignette;
    private Material _tunnelView;

    private bool _isColorReset;
    private bool _isDominantFade;

    private void Awake()
    {
        _blackScreen = _blackScreenRenderer.material;
        _vignette = _vignetteRenderer.material;
        _tunnelView = _tunnelViewRenderer.material;
        ApplyStartFaderState();
    }

    private void Start()
    {
        _isColorReset = false;
    }

    private void ApplyStartFaderState()
    {
        SetVignette(0);
        DisableTunnelView();
    }

    #region Fader

    public void FadeIn(float? timeToChange = null)
    {
        float time = 1;

        if (timeToChange != null)
            time = (float)timeToChange;

        _blackScreen.DOFade(1, time);
        
        if (_hud != null)
            _hud.DOFade(0, time);
    }

    public void FadeOut(float? timeToChange = null)
    {
        float time = 1;

        if (timeToChange != null)
            time = (float)timeToChange;

        _blackScreen.DOFade(0, time);
        
        if (_hud != null)
            _hud.DOFade(1, time);
    }

    public void SetBlackScreen()
    {
        _blackScreen.color = new Color(_blackScreen.color.r, _blackScreen.color.g, _blackScreen.color.b, 1);
        
        if (_hud != null)
            _hud.DOFade(0, 0.01f);
    }

    public void SetTransparentScreen()
    {
        _blackScreen.color = new Color(_blackScreen.color.r, _blackScreen.color.g, _blackScreen.color.b, 0);
        
        if (_hud != null)
            _hud.DOFade(1, 0.01f);
    }

    #endregion
    
    #region Loading

    public void ShowLoadingTextSlow(float? timeToChange = null)
    {
        float time = 1;

        if (timeToChange != null)
            time = (float)timeToChange;
        
        // _loading.StartAnimation();
    }
    
    public void HideLoadingTextSlow(float? timeToChange = null)
    {
        float time = 1;

        if (timeToChange != null)
            time = (float)timeToChange;
        
        // _loading.StopAnimation();
    }

    public void TurnOnLoadingText()
    {
        // _loading.StartAnimationFast();
    }
    
    public void TurnOffLoadingText()
    {
        // _loading.StopAnimationFast();
    }

    #endregion

    #region Vignette

    public void SetVignette(float intensity, Color? color = null, float? timeToChange = null)
    {
        float time = 1;

        if (timeToChange != null)
            time = (float)timeToChange;

        _vignette.DOFade(intensity, time);

        if (color != null)
            _vignette.SetColor("_EmissionColor", (Color)color);

    }

    public void EnableTunnelView()
    {
        _tunnelView.DOFade(1, 0.01f);
    }

    public void DisableTunnelView()
    {
        _tunnelView.DOFade(0, 0.01f);
    }

    private IEnumerator TestFaderCoroutine()
    {
        FadeIn();
        yield return new WaitForSeconds(2);
        FadeOut();
        yield return new WaitForSeconds(2);
        FadeIn(2);
        yield return new WaitForSeconds(5);
        FadeOut(4);
        yield return new WaitForSeconds(5);
        SetBlackScreen();
        yield return new WaitForSeconds(2);
        SetTransparentScreen();
        yield return new WaitForSeconds(2);
        EnableTunnelView();
        yield return new WaitForSeconds(2);
        DisableTunnelView();
        yield return new WaitForSeconds(2);
        SetVignette(1);
        yield return new WaitForSeconds(5);
        SetVignette(0);
        yield return new WaitForSeconds(5);
        SetVignette(0.5f);
        yield return new WaitForSeconds(5);
        SetVignette(1, new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        yield return new WaitForSeconds(5);
        SetVignette(0.3f, new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        yield return new WaitForSeconds(5);
        SetVignette(0.7f, new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        yield return new WaitForSeconds(5);
        SetVignette(0.5f, new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
    }

    #endregion
    
}
