using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class Scr_UI_WaveCanvas : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI WaveText;
    public TextMeshProUGUI WaveLabel;
    public RectMask2D WaveTextMask;
    public TextMeshProUGUI WaveTextColored;
    public Image BlackCover;
    public GameObject RemainingMonstersLabel;
    public TextMeshProUGUI RemainingMonstersText;
    public int MaxWaveCounterScale;

    [Header("Properties")]
    Canvas MyCanvas;
    RectTransform MaskRect;
    Vector2 WaveCounterDefaultOffset;
    RectTransform WaveCounterRect;

    private void Awake()
    {
        MyCanvas = GetComponent<Canvas>();
        MaskRect = WaveTextMask.GetComponent<RectTransform>();
        WaveCounterRect = WaveText.GetComponent<RectTransform>();
        WaveCounterDefaultOffset = WaveCounterRect.anchoredPosition;
    }

    private void Start()
    {
        Scr_WaveSpawner.Instance.OnEnemyAmountChange += UpdateRemainingMonsters;
    }

    public void NextWave()
    {
        if(Scr_GameManager.Instance.CurrentWave == 1)
        {
            MyCanvas.enabled = true;
            WaveIntroduction();
        }
        else
        {
            NextWaveAnimation();
        }
    }

    void WaveIntroduction()
    {
        Sequence WaveIntroAnimation = DOTween.Sequence();
        RectTransform WaveRect = WaveLabel.GetComponent<RectTransform>();
        WaveIntroAnimation.SetDelay(1);
        WaveIntroAnimation.Append(WaveRect.DOAnchorPos(Vector2.zero, 2));
        WaveIntroAnimation.Join(WaveRect.DOScale(Vector3.one, 2));

        WaveIntroAnimation.Append(WaveText.DOColor(Color.white, 1)).OnComplete(() => 
        {
            Scr_WaveSpawner.Instance.SpawnWave();
            RemainingMonstersLabel.SetActive(true);
        });

        WaveIntroAnimation.Join(DOVirtual.Float(0, MaskRect.rect.height, 1, (x) => WaveTextMask.padding = new Vector4(0,0,0,x)));
        WaveIntroAnimation.Join(DOVirtual.Float(WaveLabel.fontSize, 30, 1, (x) => WaveLabel.fontSize = x));
    }

    void NextWaveAnimation()
    {
        WaveText.text = Scr_GameManager.Instance.CurrentWave.ToString();
        WaveTextColored.text = Scr_GameManager.Instance.CurrentWave.ToString();
        WaveTextMask.padding = new Vector4(0, 0, 0, 0);
        WaveText.transform.localScale = new Vector3(MaxWaveCounterScale, MaxWaveCounterScale, 1);
        WaveCounterRect.position = new Vector2(Screen.width / 2, Screen.height - (WaveCounterRect.rect.height / 2) * MaxWaveCounterScale);
        float TotalDelayTillNextWave = Scr_WaveSpawner.Instance.TimeBetweenWaves + Scr_WaveSpawner.Instance.SlowMoDuration;
        Sequence NextWaveAnimation = DOTween.Sequence();
        NextWaveAnimation.OnComplete(() => Scr_WaveSpawner.Instance.SpawnWave());
        NextWaveAnimation.SetUpdate(true);
        NextWaveAnimation.Append(DOVirtual.Float(0, MaskRect.rect.height, TotalDelayTillNextWave, (x) => WaveTextMask.padding = new Vector4(0, 0, 0, x)));
        NextWaveAnimation.Join(WaveText.transform.DOScale(Vector3.one, 2));
        NextWaveAnimation.Join(WaveCounterRect.DOAnchorPos(WaveCounterDefaultOffset, 2));
    }

    public void GameOverAnimation()
    {
        BlackCover.color = new Color(0,0,0,0);
        BlackCover.enabled = true;
        BlackCover.DOFade(1, 3).OnComplete(Scr_UI_FinalScoreCanvas.Instance.FinalScoreAnimation);
    }

    void UpdateRemainingMonsters()
    {
        RemainingMonstersText.text = Scr_WaveSpawner.Instance.SpawnedMonsters.Count.ToString();
    }


    #region Networking

    //public void ReceivedNextWave(float _ReceiveDelay)
    //{
    //    if (Scr_GameManager.Instance.CurrentWave == 1)
    //    {
    //        MyCanvas.enabled = true;
    //        WaveIntroduction();
    //    }
    //    else
    //    {
    //        NextWaveAnimation(_ReceiveDelay);
    //    }
    //}

    //void NextWaveAnimation(float _ReceiveDelay)
    //{
    //    WaveText.text = Scr_GameManager.Instance.CurrentWave.ToString();
    //    WaveText.color = Color.red;
    //    WaveText.DOColor(Color.white, 1).SetDelay(Scr_WaveSpawner.TimeBetweenWaves - _ReceiveDelay).OnComplete(() => Scr_WaveSpawner.Instance.SpawnWave());
    //}

    #endregion
}
