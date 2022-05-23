using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Nakama;
using System;

public class Scr_UI_PlayerCanvas : Scr_UIBase
{
    [Header("Developer")]
    public float EXPSliderDuration;
    public float ScoreIncreaseDuration;

    [Header("References")]
    public TextMeshProUGUI ScoreText;
    public Slider EXPSlider;
    public GameObject ExpSliderBg;
    public GameObject EXPSliderFill;
    public TextMeshProUGUI PlayerIDText;
    public TextMeshProUGUI LevelMaxedText;
    public Scr_UI_PingCanvas PingCanvas;

    [Header("Heart References")]
    [SerializeField] GameObject HeartIconPrefab;
    [SerializeField] Sprite FullHeartSprite;
    [SerializeField] Sprite EmptyHeartSprite;
    [SerializeField] GameObject HeartPanel;
    [SerializeField] List<GameObject> HeartIconList;

    private int ActiveHeartCount;
    private int FullHeartCount;

    [Header("Properties")]
    private Sequence EXPSliderSequence;
    private Sequence ScoreSequence;
    private int ScorePoint;
    private Scr_Player AssignedPlayer;
    public void SetAssignedPlayer(Scr_Player _ThePlayer)
    {
        AssignedPlayer = _ThePlayer;
    }

    protected override void Awake()
    {
        base.Awake();

        foreach(Transform _HeartIcon in HeartPanel.GetComponentInChildren<Transform>())
        {
            HeartIconList.Add(_HeartIcon.gameObject);
        }
    }

    public void UpdateHealth(int _Health, int _MaxHealth, int _Amount)
    {
        UpdateMaxHeart(_MaxHealth);
        UpdateCurrentHeart(_Amount);
    }

    public int GetActiveHeartNumber()
    {
        int _Num = 0;

        foreach(GameObject _Icon in HeartIconList)
        {
            if(_Icon.activeSelf)
            {
                _Num++;
            }
        }

        return _Num;
    }

    public void DoTweenHeart(Transform _HeartTransform, bool IsIncrease)
    {
        var _Sequence = DOTween.Sequence();

        if (IsIncrease)
        {
            _Sequence.Append(_HeartTransform.DOScale(new Vector3(1.5f, 1.5f, 1.5f), 0.5f));

            _Sequence.OnComplete(() =>
            {
                _HeartTransform.transform.DOScale(new Vector3(1, 1, 1), 0.5f);
            });
        }
        else
        {
            _Sequence.Append(_HeartTransform.DOScale(new Vector3(0.5f, 0.5f, 0.5f), 0.5f));

            _Sequence.OnComplete(() =>
            {
                _HeartTransform.DOScale(new Vector3(1, 1, 1), 0.5f);
            });
        }
    }


    public void UpdateMaxHeart(int _MaxHealth)
    {
        int ActiveHeart = GetActiveHeartNumber();

        if (_MaxHealth != ActiveHeart)
        {
            var _Difference = _MaxHealth - ActiveHeart;

            // If MaxHealth is more than current icons.
            if (_Difference > 0)
            {
                // Enable heart icons.
                for (int i = 0; i < HeartIconList.Count; i++)
                {
                    if (_Difference == 0)
                    {
                        return;
                    }

                    if (!HeartIconList[i].activeSelf)
                    {
                        HeartIconList[i].SetActive(true);
                        DoTweenHeart(HeartIconList[i].transform, true);
                        _Difference--;
                    }
                }

                // If after enabling all hearts AND there's *STILL* difference,
                // instantiate more hearts icon.
                if (_Difference > 0)
                {
                    for (int i = 0; i < _Difference; i++)
                    {
                       GameObject _NewIcon = Instantiate(HeartIconPrefab, HeartPanel.transform);
                       HeartIconList.Add(_NewIcon);
                       DoTweenHeart(_NewIcon.transform, true);
                    }
                }

            }
            else if (_Difference < 0) // If MaxHealth is less than current icons.
            {
                // Disable heart icons.
                for (int i = HeartIconList.Count - 1; i >= 0; i--)
                {
                    if (_Difference == 0)
                    {
                        return;
                    }

                    if (HeartIconList[i].activeSelf)
                    {
                        HeartIconList[i].SetActive(false);
                        _Difference++;
                    }
                }
            }
        }
    }

    public void UpdateCurrentHeart(int _ChangeAmount)
    {
        // If health is increased.
        if (_ChangeAmount > 0)
        {
            for (int i = 0; i < HeartIconList.Count; i++)
            {
                // If the heart icon is not active.
                if (!HeartIconList[i].activeSelf)
                {
                    continue;
                }

                if(_ChangeAmount ==0)
                {
                    return;
                }

                if (HeartIconList[i].GetComponent<Image>().sprite == EmptyHeartSprite)
                {
                    HeartIconList[i].GetComponent<Image>().sprite = FullHeartSprite;
                    DoTweenHeart(HeartIconList[i].transform, true);
                    _ChangeAmount--;
                }
            }
        }
        else if (_ChangeAmount < 0) // If health is decreased.
        {
            // Heart has to reduce by reverse.
            for (int i = HeartIconList.Count -1 ; i >= 0; i--)
            {
                // If the heart icon is not active.
                if (!HeartIconList[i].activeSelf)
                {
                    continue;
                }

                if (_ChangeAmount == 0)
                {
                    return;
                }

                if (HeartIconList[i].GetComponent<Image>().sprite == FullHeartSprite)
                {
                    HeartIconList[i].GetComponent<Image>().sprite = EmptyHeartSprite;
                    DoTweenHeart(HeartIconList[i].transform, false);
                    _ChangeAmount++;
                }
            }
        }
    }

    public void UpdateScore(int _Score)
    {

        if (ScoreSequence != null)
        {
            ScoreSequence.Pause();
        }

        ScoreSequence = DOTween.Sequence();
        
        ScoreSequence.Append(
           DOTween.To(() => ScorePoint, x => ScorePoint = x, _Score, ScoreIncreaseDuration)
           .OnUpdate(UpdateScoreText));

    }

    public void UpdateScoreText()
    {
        ScoreText.text = ScorePoint.ToString();
    }

    public int GetScore()
    {
        return ScorePoint;
    }

    public void ResetSlider()
    {
        if (EXPSliderSequence != null)
        {
            EXPSliderSequence.Pause();
        }

        EXPSliderSequence = DOTween.Sequence();

        EXPSliderSequence.Append(EXPSlider.DOValue(0, EXPSliderDuration));
        EXPSliderSequence.Append(EXPSliderFill.transform.DOScale(new Vector3(0.95f, 0.5f, 0.95f), 0.3f));
        EXPSliderSequence.Append(EXPSliderFill.transform.DOScale(new Vector3(1, 1, 1), 0.3f));
    }

    public void UpdateEXP(int _CurrentEXP, int _MaxEXP, bool IsTurningMaxLevel)
    {
        float _Value = (float)_CurrentEXP / (float)_MaxEXP;
        float Duration = EXPSliderDuration;

        if (EXPSliderSequence != null)
        {
            EXPSliderSequence.Pause();
        }

        EXPSliderSequence = DOTween.Sequence();

        //EXP slider is expected to go to a value that is below its current value. e.g. level up and some extra
        if (EXPSlider.value >= _Value)
        {
            Duration /= 2;
            EXPSliderSequence.Append(EXPSlider.DOValue(1, Duration));
            EXPSliderSequence.AppendCallback(() => UpdateLevel(IsTurningMaxLevel));
            EXPSliderSequence.Append(EXPSliderFill.transform.DOScale(new Vector3(1.05f, 3f, 1.05f), 0.3f));
            EXPSliderSequence.Append(EXPSliderFill.transform.DOScale(new Vector3(1, 1, 1), 0.3f));

            if(!IsTurningMaxLevel)
            {
                EXPSliderSequence.AppendCallback(() => 
                {
                    EXPSlider.value = 0;
                });
            }
            //else
            //{
            //    EXPSliderSequence.AppendCallback(() => ShowMaxedLevelUI());
            //    return; 
            //}
        }
        else
        {
            //To check for revives
            UpdateLevel(IsTurningMaxLevel);
        }

        EXPSliderSequence.Append(EXPSlider.DOValue(_Value, Duration));       
    }

    public void UpdateLevel(bool _IsTurningMaxLevel)
    {
        if(_IsTurningMaxLevel)
        {
            ShowMaxedLevelUI(true);
        }
        else
        {
            ShowMaxedLevelUI(false);
            LevelMaxedText.text = "Level " + AssignedPlayer.CurrentLevel;
        }
    }

    void ShowMaxedLevelUI(bool _State)
    {
        //UI was already set to Maxed state
        if(LevelMaxedText.text == "Level Maxed" && _State)
        {
            return;
        }
        else if(LevelMaxedText.text != "Level Maxed" && !_State) //UI was already set to not Maxed state
        {
            return;
        }

        Color _GreyedOutColor = ExpSliderBg.GetComponent<Image>().color;
        _GreyedOutColor.a = _State ? 0.5f : 1;
        ExpSliderBg.GetComponent<Image>().color = _GreyedOutColor;

        _GreyedOutColor = EXPSliderFill.GetComponentInChildren<Image>().color;
        _GreyedOutColor.a = _State ? 0.5f : 1;
        EXPSliderFill.GetComponentInChildren<Image>().color = _GreyedOutColor;

        LevelMaxedText.text = _State ? "Level Maxed" : ""; 
        //LevelMaxedText.gameObject.SetActive(true);
    }
}
