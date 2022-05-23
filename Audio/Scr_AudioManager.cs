using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using System.Runtime.InteropServices;

public class Scr_AudioManager : MonoBehaviour
{
    [Header("Singleton")]
    public static Scr_AudioManager Instance;

    public Bus BGMBus { get; private set; }
    public Bus SFXBus { get; private set; }

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        BGMBus = FMODUnity.RuntimeManager.GetBus("bus:/BGM");
        SFXBus = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
        BGMBus.setVolume(0.5f);
        SFXBus.setVolume(0.5f);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if(!hasFocus)
        {
            BGMBus.setPaused(true);
            SFXBus.setPaused(true);
        }
        else if(hasFocus)
        {
            BGMBus.setPaused(false);
            SFXBus.setPaused(false);
        }
    }

    public void PlayOneShot2D(string _EventName)
    {
        if(string.IsNullOrEmpty(_EventName))
        {
            return;
        }

        var OneShot = FMODUnity.RuntimeManager.CreateInstance(_EventName);
        OneShot.start();
        OneShot.release();
    }

    public void PlayOneShot3D(string _EventName, Vector2 _Position)
    {
        if(string.IsNullOrEmpty(_EventName))
        {
            return;
        }

        EventInstance OneShot = FMODUnity.RuntimeManager.CreateInstance(_EventName);
        OneShot.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(_Position));
        OneShot.start();
        OneShot.release();
    }
}
