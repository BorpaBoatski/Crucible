using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scr_UIBase : MonoBehaviour
{
    [Header("FMOD")]
    static string FMODButtonSFX = "event:/SFX/UI/ButtonClick";

    [Header("UIBase Properties")]
    protected Canvas MyCanvas;

    protected virtual void Awake()
    {
        MyCanvas = GetComponent<Canvas>();
    }

    public void PlayButtonSFX()
    {
        Scr_AudioManager.Instance.PlayOneShot2D(FMODButtonSFX);
    }

    public virtual void OpenUI()
    {
        MyCanvas.enabled = true;
    }

    public virtual void CloseUI()
    {
        MyCanvas.enabled = false;
    }

    public void OnClickLeaveGame()
    {
        PlayButtonSFX();
        //Debug.Log("Leave");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

#elif PLATFORM_WEBGL
        Scr_WWW_Bridge.Instance.SEND_GAME_CLOSE();
        Application.Quit();
#else
        Application.Quit();
#endif
    }
}
