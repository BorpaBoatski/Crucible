using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using DG.Tweening;

public class Scr_Animation_2DLightFlicker : MonoBehaviour
{
    [Header("Developer")]
    public float RadiusDecrease = 0.2f;
    public float IntensityDecrease = 0.2f;
    public float TweenDuration = 1;

    [Header("Properties")]
    Light2D This2DLight;
    float LerpTimer;

    private void Awake()
    {
        This2DLight = GetComponent<Light2D>();
    }

    private void Start()
    {
        Sequence FlickerSequence = DOTween.Sequence();
        FlickerSequence.SetLoops(-1, LoopType.Yoyo);
        FlickerSequence.Append(DOTween.To(AdjustLightRadius, This2DLight.pointLightInnerRadius, This2DLight.pointLightInnerRadius - RadiusDecrease, TweenDuration));
        FlickerSequence.Join(DOTween.To(x => This2DLight.intensity = x, This2DLight.intensity, This2DLight.intensity - IntensityDecrease, TweenDuration));
    }

    void AdjustLightRadius(float _Radius)
    {
        This2DLight.pointLightOuterRadius = _Radius;
        This2DLight.pointLightInnerRadius = _Radius;
    }
}
