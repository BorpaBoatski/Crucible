using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Nakama;
using System;
using UnityEngine.Networking;

public class Scr_UI_MatchmakingCanvas : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField URLInput;
    public Button MatchmakeButton;
    public Slider LoadingSlider;
    public TextMeshProUGUI LoadingText;
    public Image ScreenCover;
    public TMP_InputField CustomIDInput;

    [FMODUnity.BankRef]
    public List<string> Banks;

    [Header("Properties")]
    AsyncOperation SceneLoading;
    public Queue<Action> LoadingOperations { get; private set; } = new Queue<Action>();
    float PauseBetweenOperations = 0.1f;

    private void Start()
    {
        LoadingOperations.Enqueue(() => StartCoroutine(LoadScene()));
        LoadingOperations.Enqueue(() => StartCoroutine(LoadFMODBanks()));
    }

    public void UpdateURL()
    {
        NakamaConnection.Instance.SetLocalPlayerSkin(URLInput.text);
    }

    public void OnClickMatchmake()
    {
        NakamaConnection.Instance.FindMatch();
        ScreenCover.gameObject.SetActive(true);
    }

    public void OnClickSinglePlay()
    {
        NakamaConnection.Instance.SinglePlay();
        ScreenCover.gameObject.SetActive(true);
    }

    IEnumerator LoadScene()
    {
        LoadingText.text = "Loading Scene...";
        SceneLoading = SceneManager.LoadSceneAsync(1);
        SceneLoading.allowSceneActivation = false;

        while (SceneLoading.progress < 0.9f)
        {
            LoadingSlider.value = SceneLoading.progress / 0.9f;
            yield return null;
        }

        LoadingSlider.value = 1;
        yield return new WaitForSecondsRealtime(PauseBetweenOperations);
        GoToNextOperation();
    }

    IEnumerator LoadFMODBanks()
    {
        LoadingText.text = "Loading Audio...";
        FMODUnity.RuntimeManager.CoreSystem.mixerSuspend();

        foreach (string B in Banks)
        {
            FMODUnity.RuntimeManager.LoadBank(B, true);
        }

        while (!FMODUnity.RuntimeManager.HaveAllBanksLoaded)
        {
            for(int i = 0; i < Banks.Count; i++)
            {
                if(!FMODUnity.RuntimeManager.HasBankLoaded(Banks[i]))
                {
                    LoadingSlider.value = i / Banks.Count;
                    break;
                }
            }

            yield return null;
        }

        LoadingSlider.value = 1;
        yield return new WaitForSecondsRealtime(PauseBetweenOperations);
        FMODUnity.RuntimeManager.CoreSystem.mixerResume();
        GoToNextOperation();
    }

    public void EnqueueLoadingOperation(Action _Action)
    {
        LoadingOperations.Enqueue(_Action);
    }

    public void DisplayLoadingMessage(string _Message)
    {
        LoadingText.text = _Message;
    }

    public void TrackProgress(float _Progress)
    {
        LoadingSlider.value = _Progress;

        if(_Progress == 1)
        {
            Invoke("GoToNextOperation", PauseBetweenOperations);
        }
    }

    public void GoToNextOperation()
    {
        if(!ScreenCover.gameObject.activeSelf)
        {
            ScreenCover.gameObject.SetActive(true);
        }

        if (LoadingOperations.Count > 0)
        {
            LoadingOperations.Dequeue().Invoke();
        }
        else
        {
            if(NakamaConnection.Instance.IsMultiplayer())
            {
                if(NakamaConnection.Instance.AmIHost())
                {
                    LoadingText.text = "Waiting for other player...";
                    NakamaConnection.Instance.PlayerReady();
                }
                else
                {
                    LoadingText.text = "Waiting for host...";
                    NakamaConnection.Instance.RequestSendMatchState(OpCodes.GameLoaded);
                }
            }
            else
            {
                StartGame();
            }
        }
    }

    public void StartGame()
    {
        SceneLoading.allowSceneActivation = true;
    }

    public void InputCustomID(string _CustomID)
    {
        if(CustomIDInput.text != _CustomID)
        {
            CustomIDInput.text = _CustomID;
        }

        NakamaConnection.Instance.SetTestCustomID(_CustomID);
    }

    public void InputUsername(string _Username)
    {
        NakamaConnection.Instance.SetTestUsername(_Username);
    }
}
