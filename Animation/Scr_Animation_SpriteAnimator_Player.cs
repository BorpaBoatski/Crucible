using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Nakama;

public class Scr_Animation_SpriteAnimator_Player : Scr_Animation_SpriteAnimator
{
    [Header("Reference")]
    public ParticleSystem SpeedBoostEffect;
    public Scr_Player ThisPlayer;

    [Header("Properties")]
    Sprite[] DownloadedSprites = new Sprite[9];
    Coroutine CurrentAnimation;

    void Start()
    {
        if(DownloadedSprites[0] != null)
        {
            StartDownloadedSpriteAnimation();
        }
        else
        {
            DefaultAnimation();
        }
    }

    public void StartDownloadedSpriteAnimation()
    {
        if (CurrentAnimation != null)
        {
            StopCoroutine(CurrentAnimation);
        }

        CurrentAnimation = StartCoroutine(Animate(DownloadedSprites));
        SpeedBoostEffect.textureSheetAnimation.SetSprite(0, DownloadedSprites[0]);
    }

    void DefaultAnimation()
    {
        if(CurrentAnimation != null)
        {
            StopCoroutine(CurrentAnimation);
        }

        CurrentAnimation = StartCoroutine(Animate(DefaultAnimationFrames));
        SpeedBoostEffect.textureSheetAnimation.SetSprite(0, DefaultAnimationFrames[0]);
    }

    public void RestartAnimation()
    {
        if(DownloadedSprites[0] != null)
        {
            StartDownloadedSpriteAnimation();
        }
        else
        {
            DefaultAnimation();
        }

    }    

    public void SetDownloadedSprites(Sprite[] _DownloadedSprites)
    {
        DownloadedSprites = _DownloadedSprites;
        StartDownloadedSpriteAnimation();
    }

    public Sprite GetDownloadedFirstFrame()
    {
        return DownloadedSprites[0];
    }

    protected override void OnEnable()
    {
        
    }
}
