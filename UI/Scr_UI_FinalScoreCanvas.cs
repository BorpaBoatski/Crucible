using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Nakama;
using System.Linq;
using UnityEngine.UI;

public class Scr_UI_FinalScoreCanvas : Scr_UIBase
{
    [Header("Singleton")]
    public static Scr_UI_FinalScoreCanvas Instance;

    [Header("References")]
    public Canvas FinalScoreCanvas;
    public GameObject TotalScore;
    public TMP_Text WaveText;
    public TMP_Text TotalScoreText;
    public Button LeaveButton;

    [Header("Player 1 References")]
    public RectTransform Player1StatsRect;
    public Scr_Player Player1;
    public TMP_Text Player1ScoreText;
    public Image Player1Sprite;

    [Header("Player 2 References")]
    public Scr_Player Player2;
    public Canvas Player2StatsCanvas;
    public TMP_Text Player2ScoreText;
    public Image Player2Sprite;

    [Header("Sequences")]
    private int Player1AnimatingScore = 0;
    private int Player2AnimatingScore = 0;
    private int TotalAnimatingScore = 0;

    private Sequence ScoreSequence;

    [Header("Properties")]
    CanvasGroup FinalScoreGroup;
    [SerializeField] private float ScoreIncreaseDuration;

    protected override void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FinalScoreGroup = GetComponent<CanvasGroup>();
    }

    public void FinalScoreAnimation()
    {
#if PLATFORM_WEBGL && !UNITY_EDITOR
        SendScoreToBrowser();
#elif UNITY_EDITOR
        TestSendScoreToBrowser();
#endif

        FinalScoreCanvas.enabled = true;
        ScoreSequence = DOTween.Sequence();
        ScoreSequence.Append(FinalScoreCanvas.transform.
                             DOMoveY(Screen.height/2, 0.5f));

        // Update player sprite
        Scr_Animation_SpriteAnimator_Player Player1Animator = Player1.MyRenderer.GetComponent<Scr_Animation_SpriteAnimator_Player>();
        if (Player1Animator.GetDownloadedFirstFrame())
        {
            Player1Sprite.sprite = Player1Animator.GetDownloadedFirstFrame();
        }
        else
        {
            Player1Sprite.sprite = Player1Animator.DefaultAnimationFrames[0];
        }

        if (!NakamaConnection.Instance.IsMultiplayer())
        {
            //AdjustPlayer1ToCenter();
        }
        else
        {
            Player2StatsCanvas.enabled = true;

            //Update player 2 sprite.
            Scr_Animation_SpriteAnimator_Player Player2Animator = Player2.MyRenderer.GetComponent<Scr_Animation_SpriteAnimator_Player>();
            if (Player2Animator.GetDownloadedFirstFrame())
            {
                Player2Sprite.sprite = Player2Animator.GetDownloadedFirstFrame();
            }
            else
            {
                Player2Sprite.sprite = Player2Animator.DefaultAnimationFrames[0];
            }

        }

        FinalScoreGroup.DOFade(1, 1);
        UpdateEndingStats();
    }

    void AdjustPlayer1ToCenter()
    {
        Player1StatsRect.anchoredPosition = new Vector2(0, 0);
    }

    void UpdateEndingStats()
    {
        DoTweenPlayer1Score();
        int _TotalScore = Player1.GetScore();

        if (NakamaConnection.Instance.IsMultiplayer())
        {
            DoTweenPlayer2Score();
            _TotalScore += Player2.GetScore();
            TotalScore.SetActive(true);
            DoTweenFinalScore(_TotalScore);
        }

        //DoTweenFinalScore(_TotalScore);

        WaveText.text = (Scr_GameManager.Instance.CurrentWave - 1).ToString();
        //WaveText.text = "Survived " + Scr_GameManager.Instance.CurrentWave.ToString() + " Rounds";       
    }

    /// <summary>
    /// Used to create the JSON neceesary for browser to receive game's score. Check with FinalScoreAnimation method for this method execution.
    /// </summary>
    void SendScoreToBrowser()
    {
        GAME_STOPPED_NO_ERROR_DATA FinalResultsData = new GAME_STOPPED_NO_ERROR_DATA();
        FinalResultsData.waves = Scr_GameManager.Instance.CurrentWave - 1;
        FinalResultsData.score = Player1.GetScore();
        FinalResultsData.ranks = new string[Scr_GameManager.Instance.Players.Count];

        for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
        {
            FinalResultsData.ranks[i] = Scr_GameManager.Instance.Players[i].PlayerUsername;
        }

        if (NakamaConnection.Instance.IsMultiplayer())
        {
            FinalResultsData.score += Player2.GetScore();
        }

        GAME_STOPPED_NO_ERROR_MESSAGE FinalResults = new GAME_STOPPED_NO_ERROR_MESSAGE(FinalResultsData, "CRUCIBLE_GAME_COMPLETED", "Crucible game has been completed");
        Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED(FinalResults);
    }

    void TestSendScoreToBrowser()
    {
        GAME_STOPPED_NO_ERROR_DATA FinalResultsData = new GAME_STOPPED_NO_ERROR_DATA();
        FinalResultsData.waves = Scr_GameManager.Instance.CurrentWave - 1;
        FinalResultsData.score = Player1.GetScore();
        FinalResultsData.ranks = new string[Scr_GameManager.Instance.Players.Count];

        for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
        {
            FinalResultsData.ranks[i] = Scr_GameManager.Instance.Players[i].PlayerUsername;
        }

        if (NakamaConnection.Instance.IsMultiplayer())
        {
            FinalResultsData.score += Player2.GetScore();
        }

        GAME_STOPPED_NO_ERROR_MESSAGE FinalResults = new GAME_STOPPED_NO_ERROR_MESSAGE(FinalResultsData, "CRUCIBLE_GAME_COMPLETED", "Crucible game has been completed");
        Debug.Log(JsonUtility.ToJson(FinalResults, true));
        //Debug.Log(JsonUtility.ToJson(FinalResults));
    }

    public void DoTweenPlayer1Score()
    {   
        ScoreSequence.Append(DOTween.To(() =>
                                    Player1AnimatingScore, x => Player1AnimatingScore = x, Player1.GetScore(), ScoreIncreaseDuration)
                                    .OnUpdate(UpdatePlayer1ScoreText));
    }

    public void DoTweenPlayer2Score()
    {

        ScoreSequence.Append(DOTween.To(() =>
                                    Player2AnimatingScore, x => Player2AnimatingScore = x, Player2.GetScore(), ScoreIncreaseDuration)
                                   .OnUpdate(UpdatePlayer2ScoreText));
    }

    public void DoTweenFinalScore(int _TotalScore)
    {

        ScoreSequence.Append(DOTween.To(() =>
                                   TotalAnimatingScore, x => TotalAnimatingScore = x, _TotalScore, ScoreIncreaseDuration)
                                  .OnUpdate(UpdateTotalScoreText)).OnComplete(() => 
                                  {
                                        LeaveButton.interactable = true;
                                  });
    }

    public void UpdatePlayer1ScoreText()
    {
        Player1ScoreText.text = Player1AnimatingScore.ToString();
    }

    public void UpdatePlayer2ScoreText()
    {
        Player2ScoreText.text = Player2AnimatingScore.ToString();
    }

    public void UpdateTotalScoreText()
    {
        TotalScoreText.text = TotalAnimatingScore.ToString();
    }
}
