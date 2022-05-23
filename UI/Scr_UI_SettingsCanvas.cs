using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scr_UI_SettingsCanvas : Scr_UIBase
{
    [Header("References")]
    public Slider SFXSlider;
    public Slider BGMSlider;
    public GameObject InputBlocker;
    public GameObject SettingsWindow;

    [Header("Properties")]
    Coroutine SFXTestPlay;

    protected override void Awake()
    {
        MyCanvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        float SFXVolume;
        float BGMVolume;

        if(Scr_AudioManager.Instance.BGMBus.getVolume(out SFXVolume) == FMOD.RESULT.OK)
        {
            SFXSlider.value = SFXVolume;
        }

        if (Scr_AudioManager.Instance.SFXBus.getVolume(out BGMVolume) == FMOD.RESULT.OK)
        {
            BGMSlider.value = BGMVolume;
        }
    }

    public void OnClickToggleUI()
    {
        PlayButtonSFX();
        SettingsWindow.SetActive(!SettingsWindow.activeSelf);
    }

    public void OnSliderChangeSFX(float _NewSFX)
    {
        Scr_AudioManager.Instance.SFXBus.setVolume(_NewSFX);

        if(SFXTestPlay != null)
        {
            StopCoroutine(SFXTestPlay);
        }

        SFXTestPlay = StartCoroutine(SFXTest());
    }

    public void OnSliderChangeBGM(float _NewBGM)
    {
        Scr_AudioManager.Instance.BGMBus.setVolume(_NewBGM);
    }

    IEnumerator SFXTest()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        PlayButtonSFX();
    }
}
