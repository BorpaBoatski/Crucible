using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;

public class NakamaConnection : MonoBehaviour
{
    [Header("Player Account")]
    public string SkinURL;
    public Scr_Database_ClientConnection OasisConnection;
    public Scr_Database_ClientConnection TestingConnection;
    string CustomID = "0x1ac8d00670311843645f00a3ca33d249e03addcb";
    string TestUsername;

    [Header("References")]
    public Scr_UI_MatchmakingCanvas MatchmakingCanvas;

    public static NakamaConnection Instance;
    IClient client;
    public ISession session { get; private set; }
    ISocket socket;
    string matchID;
    bool OtherPlayerLoaded;
    bool SentLoadingComplete;
    public string HostID { get; private set; }
    public IMatch ConnectedMatch { get; private set; }
    Queue<Action> QueuedSendStateRequests = new Queue<Action>();
    public int RoomSize { get; private set; }

    [Header("Properties")]
    int LoadedPlayers;
    Coroutine RemindCharacterSpriteCoroutine;

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

    /// <summary>
    /// Authenticate user using custom nakama authentication. Retrieve ID from Oasis
    /// </summary>
    public async Task TestAuthenticateClient()
    {
        //Oasis connection test
        Debug.Log(TestingConnection.host);
        client = new Client(TestingConnection.scheme, TestingConnection.host, TestingConnection.port, TestingConnection.serverKey, UnityWebRequestAdapter.Instance);

        if(TestingConnection.host == "localhost")
        {
            session = await client.AuthenticateDeviceAsync(System.Guid.NewGuid().ToString());
        }
        else
        {
            session = await client.AuthenticateCustomAsync(CustomID, create: false);
        }

        //Debug.Log(session.UserId);
        socket = client.NewSocket();
        await socket.ConnectAsync(session, true);
        socket.ReceivedMatchmakerMatched += OnReceivedMatchmakerMatched;
        socket.ReceivedMatchPresence += OnReceivedMatchPresence;
        socket.ReceivedMatchState += ReceiveData;
    }

    /// <summary>
    /// Authenticate user using custom nakama authentication. Retrieve ID from Oasis
    /// </summary>
    public async Task AuthenticateClient(GameInitMessage _InitMessage)
    {
        SetLocalPlayerSkin(_InitMessage.data.skin);

        if(string.IsNullOrEmpty(_InitMessage.matchmakingToken))
        {
            RoomSize = 1;
            //MatchmakingCanvas.OnClickSinglePlay();
            MatchmakingCanvas.GoToNextOperation();
            return;
        }

        //Create client and socket
        //Debug.Log(OasisConnection.scheme);
        //Debug.Log(OasisConnection.port);
        client = new Client(OasisConnection.scheme, OasisConnection.host, OasisConnection.port, OasisConnection.serverKey, UnityWebRequestAdapter.Instance);
        session = await client.AuthenticateCustomAsync(_InitMessage.data.profile, create: false);
        socket = client.NewSocket();
        //Debug.Log(session.UserId);
        await socket.ConnectAsync(session, true);
        socket.ReceivedMatchmakerMatched += OnReceivedMatchmakerMatched;
        socket.ReceivedMatchPresence += OnReceivedMatchPresence;
        socket.ReceivedMatchState += ReceiveData;
        await socket.JoinMatchAsync(_InitMessage.matchmakingToken);
    }

    public async void FindMatch()
    {
        await TestAuthenticateClient();
        //Send matchmaking ticket
        var matchmakingTicket = await socket.AddMatchmakerAsync("*", 2, 2);
    }

    public async void SinglePlay()
    {
        await TestAuthenticateClient();
        MatchmakingCanvas.GoToNextOperation();
    }

    void OnReceivedMatchPresence(IMatchPresenceEvent _MatchPresence)
    {
        if(_MatchPresence.Leaves.Count() == 0)
        {
            return;
        }

        Scr_MainThreadDispatcher.Instance.Enqueue(() => 
        {
            foreach (IUserPresence Leaves in _MatchPresence.Leaves)
            {
                DisconnectPlayer(Leaves.Username);
            }
        });
    }

    void DisconnectPlayer(string _DisconnectedPlayerUsername)
    {
        for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
        {
            Scr_Player Player = Scr_GameManager.Instance.Players[i].GetComponent<Scr_Player>();

            if (Player.PlayerUsername == _DisconnectedPlayerUsername)
            {
                Player.Disconnect();
                Scr_GameManager.Instance.Players[i] = null;
                RoomSize--;
            }
        }

        Scr_GameManager.Instance.SwitchToSinglePlayer();
    }

    async void OnReceivedMatchmakerMatched(IMatchmakerMatched matchmakerMatched)
    {
        ConnectedMatch = await socket.JoinMatchAsync(matchmakerMatched);
        //Debug.Log("Match found");
        matchID = ConnectedMatch.Id;
        HostID = matchmakerMatched.Users.First().Presence.Username;
        //Debug.Log(ConnectedMatch.Self.Username);
        RoomSize = matchmakerMatched.Users.Count();
        Scr_MainThreadDispatcher.Instance.Enqueue(() => RemindCharacterSpriteCoroutine = StartCoroutine(RemindCharacterSprite()));
        RequestSendMatchState(Encoding.UTF8.GetBytes(SkinURL), OpCodes.CharacterSprite);
    }

    IEnumerator RemindCharacterSprite()
    {
        yield return new WaitForSecondsRealtime(2);
        RequestSendMatchState(OpCodes.RemindCharacterSprite);
        RemindCharacterSpriteCoroutine = StartCoroutine(RemindCharacterSprite());
    }

    public async void RequestSendMatchState(byte[] _Data, long _Code)
    {
        //Debug.Log("Send Match " + socket.IsConnected);

        try
        {
            await socket.SendMatchStateAsync(matchID, (int)_Code, _Data, null);
        }
        catch(SocketException)
        {
            for(int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
            {
                Scr_Player Player = Scr_GameManager.Instance.Players[i];
                if (!IsLocalPlayer(Player.PlayerUsername))
                {
                    DisconnectPlayer(Player.PlayerUsername);
                    i--;
                }
            }
            //Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED_ERROR(ErrorCodes.Disconnect);
            //Application.Quit();
            //Debug.LogError("Caught socket exception");
        }
        catch(OperationCanceledException)
        {
            for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
            {
                Scr_Player Player = Scr_GameManager.Instance.Players[i];
                if (!IsLocalPlayer(Player.PlayerUsername))
                {
                    DisconnectPlayer(Player.PlayerUsername);
                    i--;
                }
            }
            //Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED_ERROR(ErrorCodes.Disconnect);
            //Application.Quit();
            //Debug.LogError("Caught OperationCanceledException");
        }
    }

    public async void RequestSendMatchState(long _Code)
    {
        //Debug.Log("Send Match " + socket.IsConnected);

        try
        {
            await socket.SendMatchStateAsync(matchID, (int)_Code, "", null);
        }
        catch (SocketException)
        {
            for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
            {
                Scr_Player Player = Scr_GameManager.Instance.Players[i];
                if (!IsLocalPlayer(Player.PlayerUsername))
                {
                    DisconnectPlayer(Player.PlayerUsername);
                    i--;
                }
            }
            //Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED_ERROR(ErrorCodes.Disconnect);
            //Application.Quit();
            //Debug.LogError("Caught socket exception");
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
            {
                Scr_Player Player = Scr_GameManager.Instance.Players[i];
                if (!IsLocalPlayer(Player.PlayerUsername))
                {
                    DisconnectPlayer(Player.PlayerUsername);
                    i--;
                }
            }
            //Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED_ERROR(ErrorCodes.Disconnect);
            //Application.Quit();
            //Debug.LogError("Caught OperationCanceledException");
        }
    }

    public void AddToReceiver(System.Action<IMatchState> _Action)
    {
        socket.ReceivedMatchState += _Action;
    }

    public void RemoveFromReceiver(System.Action<IMatchState> _Action)
    {
        socket.ReceivedMatchState -= _Action;
    }

    public bool AmIHost()
    {
        return !IsMultiplayer() || (HostID == ConnectedMatch.Self.Username);
    }

    public bool IsLocalPlayer(string _PlayerUsername)
    {
        return !IsMultiplayer() || (ConnectedMatch.Self.Username == _PlayerUsername);
    }

    public void SetLocalPlayerSkin(string _URL)
    {
        //No skin was given and for testing purposes, the skin URL was not removed to be nothing
        if(string.IsNullOrEmpty(_URL) && string.IsNullOrEmpty(SkinURL))
        {
            return;
        }

        if(MatchmakingCanvas != null)
        {
            MatchmakingCanvas.URLInput.text = _URL;
        }

        SkinURL = _URL;
        Scr_WWW_SpriteDownloader TheSpriteDownloader = Scr_WWW_SpriteDownloader.Instance;
        MatchmakingCanvas.EnqueueLoadingOperation(() => TheSpriteDownloader.SetURL(SkinURL, "Local"));
    }

    void ReceiveData(IMatchState _MatchState)
    {
        //Debug.Log("Received " + socket.IsConnected);

        try
        {
            switch (_MatchState.OpCode)
            {
                case OpCodes.CharacterSprite:
                    //Debug.Log("Received Character Sprite");
                    string ReceivedURL = MatchDataJSON.DecryptStateToString(_MatchState.State);
                    Scr_WWW_SpriteDownloader TheSpriteDownloader = Scr_WWW_SpriteDownloader.Instance;

                    Scr_MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        StopCoroutine(RemindCharacterSpriteCoroutine);

                        if (!string.IsNullOrEmpty(ReceivedURL))
                        {
                            MatchmakingCanvas.EnqueueLoadingOperation(() => TheSpriteDownloader.SetURL(ReceivedURL, _MatchState.UserPresence.Username));
                            //Debug.Log("Start Loading Operation");
                            MatchmakingCanvas.GoToNextOperation();
                        }
                        else
                        {
                            MatchmakingCanvas.GoToNextOperation();
                        }
                    });

                    break;
                case OpCodes.GameLoaded:
                    PlayerReady();
                    break;
                case OpCodes.StartGame:
                    Scr_MainThreadDispatcher.Instance.Enqueue(() => MatchmakingCanvas.StartGame());
                    break;
                case OpCodes.RemindCharacterSprite:
                    RequestSendMatchState(Encoding.UTF8.GetBytes(SkinURL), OpCodes.CharacterSprite);
                    break;
            }
        }
        catch (SocketException)
        {
            for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
            {
                Scr_Player Player = Scr_GameManager.Instance.Players[i];
                if (!IsLocalPlayer(Player.PlayerUsername))
                {
                    DisconnectPlayer(Player.PlayerUsername);
                    i--;
                }
            }
            //Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED_ERROR(ErrorCodes.Disconnect);
            //Application.Quit();
            //Debug.LogError("Caught socket exception");
        }
        catch (OperationCanceledException)
        {
            for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
            {
                Scr_Player Player = Scr_GameManager.Instance.Players[i];
                if (!IsLocalPlayer(Player.PlayerUsername))
                {
                    DisconnectPlayer(Player.PlayerUsername);
                    i--;
                }
            }
            //Scr_WWW_Bridge.Instance.SEND_GAME_STOPPED_ERROR(ErrorCodes.Disconnect);
            //Application.Quit();
            //Debug.LogError("Caught OperationCanceledException");
        }
    }

    public void PlayerReady()
    {
        LoadedPlayers++;

        if(LoadedPlayers == RoomSize)
        {
            RequestSendMatchState(OpCodes.StartGame);
            Scr_MainThreadDispatcher.Instance.Enqueue(() => MatchmakingCanvas.StartGame());
        }
    }

    public void SetTestCustomID(string _CustomID)
    {
        CustomID = _CustomID;
    }

    public void SetTestUsername(string _Username)
    {
        TestUsername = _Username;
    }

    public bool IsMultiplayer()
    {
        return RoomSize > 1;
    }

    private void OnApplicationQuit()
    {
        if(socket != null && socket.IsConnected)
        {
            if (ConnectedMatch != null)
            {
                socket.LeaveMatchAsync(ConnectedMatch);
            }

            socket.CloseAsync();
        }
    }
}
