using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Scr_WWW_SpriteDownloader : MonoBehaviour
{
    [Header("Singleton")]
    public static Scr_WWW_SpriteDownloader Instance;

    [Header("Reference")]
    public Scr_UI_MatchmakingCanvas MatchmakingCanvas;

    [Header("Properties")]
    int DownloadTries = 0;
    static int MaximumDownloadTries = 5;
    static int PPI = 70;
    static Vector2 SpriteSize = new Vector2(288, 192);
    public UnityWebRequest SkinRequest;
    public Dictionary<string, Sprite[]> PlayerSprites { get; private set; } = new Dictionary<string, Sprite[]>();

    #region Delegates

    public Delegates.PassStringDelegate OnDownloadMessage;
    public Delegates.PassFloatDelegate OnUpdate;

    #endregion

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator DownloadSprites(string _URL, string _SessionID)
    {
        if (DownloadTries >= MaximumDownloadTries)
        {
            OnDownloadMessage?.Invoke("Failed to download Character Sprite. Reverting to default sprite");
            OnUpdate?.Invoke(1);
            yield break;
        }

        UnityWebRequest SkinRequest = UnityWebRequestTexture.GetTexture(_URL);
        SkinRequest.SendWebRequest();

        while (SkinRequest.result == UnityWebRequest.Result.InProgress)
        {
            OnDownloadMessage?.Invoke("Downloading Character...");
            OnUpdate?.Invoke(SkinRequest.downloadProgress / 1.1f);
            yield return null;
        }

        if (SkinRequest.result != UnityWebRequest.Result.Success)
        {
            int RetryTimer = 1 + DownloadTries;
            DownloadTries++;

            while(RetryTimer > 0)
            {
                OnDownloadMessage?.Invoke("Could not connect to skin database. Retrying in " + RetryTimer.ToString());
                yield return new WaitForSecondsRealtime(1);
                RetryTimer -= 1;
            }

            StartCoroutine(DownloadSprites(_URL, _SessionID));
            yield break;
        }
        else
        {
            Sprite[] SlicedSprites = new Sprite[9];

            Texture2D ReceivedTexture = ((DownloadHandlerTexture)SkinRequest.downloadHandler).texture;
            int CurrentRow = 1;
            int CurrentColumn = 0;

            for (int i = 0; i < 9; i++)
            {
                if (CurrentColumn * SpriteSize.x >= ReceivedTexture.width)
                {
                    CurrentColumn = 0;
                    CurrentRow--;
                }

                SlicedSprites[i] = Sprite.Create(ReceivedTexture, new Rect((Vector2.up * CurrentRow * SpriteSize.y) + (Vector2.right * CurrentColumn * SpriteSize.x), SpriteSize), new Vector2(0.5f, .4f), PPI);
                SlicedSprites[i].name = _URL + " " + i;
                CurrentColumn++;
            }

            PlayerSprites.Add(_SessionID, SlicedSprites);
            OnDownloadMessage?.Invoke("Download Successful");
            OnUpdate?.Invoke(1);
        }
    }

    public void SetURL(string _URL, string _SessionID)
    {
        ClearDelegates();

        if(string.IsNullOrEmpty(_URL))
        {
            return;
        }

        DownloadTries = 0;
        OnDownloadMessage = MatchmakingCanvas.DisplayLoadingMessage;
        OnUpdate = MatchmakingCanvas.TrackProgress;
        StartCoroutine(DownloadSprites(_URL, _SessionID));
    }

    void ClearDelegates()
    {
        OnDownloadMessage = null;
        OnUpdate = null;
    }
}
