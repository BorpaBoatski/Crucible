using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class Scr_UI_SkillCanvas : Scr_UIBase
{
    [Header("Singleton")]
    public static Scr_UI_SkillCanvas Instance;

    [Header("References")]
    public TextMeshProUGUI Skill1Text;
    public TextMeshProUGUI Skill2Text;
    public TextMeshProUGUI Skill3Text;
    public TextMeshProUGUI Skill4Text;
    public TextMeshProUGUI UnusedPointText;

    [Header("Properties")]
    RectTransform MyRect;
    Scr_Player AssignedPlayer;
    Button Skill1Button;
    Button Skill2Button;
    Button Skill3Button;
    Button Skill4Button;

    protected override void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        base.Awake();
        MyRect = GetComponent<RectTransform>();
        Skill1Button = Skill1Text.GetComponentInParent<Button>();
        Skill2Button = Skill2Text.GetComponentInParent<Button>();
        Skill3Button = Skill3Text.GetComponentInParent<Button>();
        Skill4Button = Skill4Text.GetComponentInParent<Button>();
    }

    /// <summary>
    /// Opens the UI
    /// </summary>
    public override void OpenUI()
    {
        bool PlayerHasSkillPoint = AssignedPlayer.UnusedSkill > 0;

        if(!PlayerHasSkillPoint)
        {
            return; 
        }

        ToggleButtons(PlayerHasSkillPoint);

        if (Scr_WaveSpawner.Instance.CurrentWaveStage == Enum_WaveStage.FIGHTING || MyCanvas.enabled)
        {
            return;
        }

        base.OpenUI();

        Vector2 _ShowPosition = MyRect.anchoredPosition;
        _ShowPosition.x += MyRect.rect.width;
        MyRect.DOAnchorPosX(_ShowPosition.x, 0.5f);
    }

    /// <summary>
    /// Closes the UI
    /// </summary>
    public override void CloseUI()
    {
        if (!MyCanvas.enabled)
        {
            return;
        }

        ToggleButtons(false);

        Vector2 _HidePosition = MyRect.anchoredPosition;
        _HidePosition.x -= MyRect.rect.width;
        MyRect.DOAnchorPosX(_HidePosition.x, 0.5f).OnComplete(() => base.CloseUI());
    }

    /// <summary>
    /// Sets who this skill canvas will affect
    /// </summary>
    /// <param name="_AssignPlayer"></param>
    public void AssignPlayer(Scr_Player _AssignPlayer)
    {
        AssignedPlayer = _AssignPlayer;
        UpdateSkillsText();
    }

    /// <summary>
    /// Player clicks on skill button to upgrade it
    /// </summary>
    /// <param name="_Skill"></param>
    public void OnClickAddSkill(int _Skill)
    {
        AssignedPlayer.AddSkill(_Skill);
        ToggleButtons(AssignedPlayer.UnusedSkill > 0 ? true : false);
    }

    /// <summary>
    /// Updates the "Lv. 1" text
    /// </summary>
    public void UpdateSkillsText()
    {
        Skill1Text.text = AssignedPlayer.MyHealth.HealthLevel.ToString();
        Skill2Text.text = AssignedPlayer.MyMovement.Level.ToString();
        Skill3Text.text = AssignedPlayer.MyShooting.FireRateLevel.ToString();
        Skill4Text.text = AssignedPlayer.MyShooting.DamageLevel.ToString();
    }

    public void UpdateUnusedText()
    {
        UnusedPointText.text = AssignedPlayer.UnusedSkill.ToString();
    }

    /// <summary>
    /// Toggles the interacble state of the buttons based on _State or if skill has reached max level
    /// </summary>
    /// <param name="_State"></param>
    void ToggleButtons(bool _State)
    {
        Skill1Button.interactable = _State ? (AssignedPlayer.MyHealth.HealthLevel < AssignedPlayer.MyHealth.MaxHealthLevel ? true : false) : _State;
        Skill2Button.interactable = _State ? (AssignedPlayer.MyMovement.Level < AssignedPlayer.MyMovement.MaxLevel ? true : false) : _State;
        Skill3Button.interactable = _State ? (AssignedPlayer.MyShooting.FireRateLevel < AssignedPlayer.MyShooting.MaxFireRateLevel ? true : false) : _State;
        Skill4Button.interactable = _State ? (AssignedPlayer.MyShooting.DamageLevel < AssignedPlayer.MyShooting.MaxDamageLevel ? true : false) : _State;
    }

    #region TestingShortcuts

    /// <summary>
    /// For testing purposes
    /// </summary>
    [ContextMenu("Test Open UI")]
    public void TestOpenUI()
    {
        if(MyCanvas.enabled)
        {
            return;
        }

        MyCanvas.enabled = true;
        Vector2 _ShowPosition = MyRect.anchoredPosition;
        _ShowPosition.x += MyRect.rect.width;
        MyRect.DOAnchorPosX(_ShowPosition.x, 1);
    }

    /// <summary>
    /// For testing purposes
    /// </summary>
    [ContextMenu("Test Close UI")]
    public void TestCloseUI()
    {
        if(!MyCanvas.enabled)
        {
            return;
        }

        Vector2 _HidePosition = MyRect.anchoredPosition;
        _HidePosition.x -= MyRect.rect.width;
        MyRect.DOAnchorPosX(_HidePosition.x, 1).OnComplete(() => MyCanvas.enabled = false);
    }

    #endregion
}
