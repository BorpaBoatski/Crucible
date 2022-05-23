using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


public class Scr_UI_OnscreenEffect : MonoBehaviour
{
    public static Scr_UI_OnscreenEffect Instance;

    [Header("Vignette Reference")]
    public Volume VolumeProfile;
    public Vignette PpVignette;

    [Header("Vignette Properties")]
    public Color HealColor;
    public Color DamageColor;
    public Color InvulColor;
    public Color DyingColor;
    public float FadeAmount;

    [Header("Speed Line Particle Effect")]
    public GameObject SpeedLineParticleSystem;

    bool IsInvul;
    bool IsDying;

    private void Awake()
    {
        if(Instance !=null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;    
        }

        Vignette vig;
        if (VolumeProfile.profile.TryGet<Vignette>(out vig))
        {
            PpVignette = vig;
        }
    }

    public void PlayHurtVignette()
    {
        StopCoroutine(VignetteFade());

        PpVignette.intensity.Override(0.5f);
        PpVignette.color.Override(DamageColor);
        StartCoroutine(VignetteFade());
    }

    public void PlayHealVignette()
    {
        StopCoroutine(VignetteFade());

        PpVignette.intensity.Override(0.5f);
        PpVignette.color.Override(HealColor);
        StartCoroutine(VignetteFade());
    }

    public IEnumerator VignetteFade()
    {
        yield return new WaitForSeconds(0.1f);

        PpVignette.intensity.Override(PpVignette.intensity.value - FadeAmount);

        if (PpVignette.intensity.value >= 0)
        {
            StartCoroutine(VignetteFade());
        }
        else
        {
            CheckConstantEffect();
        }
    }

    public void CheckConstantEffect()
    {
        if(IsInvul)
        {
            PlayInvulVignette();
        }
        else if(IsDying)
        {
            PlayDyingVignette();
        }
    }

    public void PlayInvulVignette()
    {
        IsInvul = true;
        PpVignette.intensity.Override(0.5f);
        PpVignette.color.Override(InvulColor);
    }

    public void StopInvulVignette()
    {
        IsInvul = false;
        PpVignette.intensity.Override(0.0f);
        CheckConstantEffect();
    }

    public void PlayDyingVignette()
    {
        IsDying = true;
        PpVignette.intensity.Override(0.5f);
        PpVignette.color.Override(DyingColor);
    }

    public void StopDyingVignette()
    {
        IsDying = false;
        PpVignette.intensity.Override(0.0f);
    }

    public void ToggleSpeedLineParticleSystem(bool IsActive)
    {
        SpeedLineParticleSystem.SetActive(IsActive);
    }


}
