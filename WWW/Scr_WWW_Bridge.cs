using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[System.Serializable]
public class GameInitMessage
{
    public string code = "ERR_GAMEPLAY";
    public string matchmakingToken;
    public string accessToken;
    public GameInitData data;
}

[System.Serializable]
public class GameInitData
{
    public string profile;
    public string skin;
}

[System.Serializable]
public class GAME_STOPPED_ERROR
{
    public GAME_STOPPED_ERROR(string _Title, string _Description)
    {
        title = _Title;
        description = _Description;
    }

    public string code = "ERR_GAMEPLAY";
    public string title;
    public string description;
    public int type = 2;
}

[System.Serializable]
public class GAME_STOPPED_NO_ERROR_MESSAGE
{
    public GAME_STOPPED_NO_ERROR_MESSAGE(GAME_STOPPED_NO_ERROR_DATA _Data, string _Title, string _Description)
    {
        data = _Data;
        title = _Title;
        description = _Description;
    }

    public string code = "ERR_NONE";
    public string title;
    public string description;
    public int type = 1;
    public GAME_STOPPED_NO_ERROR_DATA data;
}

[System.Serializable]
public class GAME_STOPPED_NO_ERROR_DATA
{
    public string matchId;
    public string timestamp;
    public string[] ranks;
    public int waves;
    public int score;
}

public class Scr_WWW_Bridge : MonoBehaviour
{
    public static Scr_WWW_Bridge Instance;

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

    private void Start()
    {
        Application.logMessageReceived += LogMessageReceived;

#if !UNITY_EDITOR && PLATFORM_WEBGL
        SEND_GAME_LOADED();
#endif
    }

    public void SEND_GAME_LOADED()
    {
        WebGLPluginJS.GAME_LOADED();
    }

    public void RECEIVE_GAME_INIT(string _GameInitJSON)
    {
        GameInitMessage ReceivedInitMessage = JsonUtility.FromJson<GameInitMessage>(_GameInitJSON);
        NakamaConnection.Instance.AuthenticateClient(ReceivedInitMessage);
    }

    public void SEND_GAME_STOPPED_ERROR(GAME_STOPPED_ERROR _Error)
    {
#if !UNITY_EDITOR && PLATFORM_WEBGL
        WebGLPluginJS.GAME_STOPPED_ERROR(JsonUtility.ToJson(_Error));
#endif
    }

    public void SEND_GAME_STOPPED(GAME_STOPPED_NO_ERROR_MESSAGE _Results)
    {
#if !UNITY_EDITOR && PLATFORM_WEBGL
        WebGLPluginJS.GAME_STOPPED(JsonUtility.ToJson(_Results));
#endif
    }

    void LogMessageReceived(string condition, string StackTrace, LogType type)
    {
        if(type == LogType.Error)
        {
            SEND_GAME_STOPPED_ERROR(new GAME_STOPPED_ERROR("Crucible Error", condition));
        }
    }

    public void SEND_GAME_CLOSE()
    {
        WebGLPluginJS.GAME_CLOSE();
    }

    public void RECEIVE_CUSTOMID(string _Data)
    {
        if(NakamaConnection.Instance.MatchmakingCanvas == null)
        {
            return;
        }

        NakamaConnection.Instance.MatchmakingCanvas.InputCustomID(_Data);
    }

    public void RECEIVE_SKINURL(string _Data)
    {
        if (NakamaConnection.Instance.MatchmakingCanvas == null)
        {
            return;
        }

        NakamaConnection.Instance.SetLocalPlayerSkin(_Data);
    }

    public void TOGGLEINPUTCAPTURE(int _Focus)
    {
#if PLATFORM_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = _Focus == 1? true : false;
#endif
    }
}
