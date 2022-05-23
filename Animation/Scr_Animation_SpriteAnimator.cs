using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scr_Animation_SpriteAnimator : MonoBehaviour
{
    [Header("Developer")]
    public Sprite[] DefaultAnimationFrames;

    [Header("References")]
    public SpriteRenderer EffectRenderer;

    [Header("Properties")]
    protected SpriteRenderer Renderer;
    protected int CurrentSpriteFrame = 0;

    private void Awake()
    {
        Renderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void OnEnable()
    {
        StartCoroutine(Animate(DefaultAnimationFrames));
    }

    protected virtual IEnumerator Animate(Sprite[] _Frames)
    {
        CurrentSpriteFrame = 0;

        while (true)
        {
            Renderer.sprite = _Frames[CurrentSpriteFrame];
            EffectRenderer.sprite = _Frames[CurrentSpriteFrame];
            CurrentSpriteFrame = (CurrentSpriteFrame + 1) % _Frames.Length;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
