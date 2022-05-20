using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nakama;
using System.Windows;

public class Scr_GameManager : MonoBehaviour, INetworkComponent
{
    [Header("Singleton")]
    public static Scr_GameManager Instance;

    [Header("References")]
    public List<Scr_Player> Players = new List<Scr_Player>();
    public Scr_UI_WaveCanvas WaveCanvas;
    public BoxCollider2D PlayerSpawnZone;
    public GameObject NetworkPlayerPrefab;
    public GameObject LocalPlayerPrefab;
    public Scr_UI_PlayerCanvas Player1Canvas;
    public Scr_UI_PlayerCanvas Player2Canvas;
    public Scr_UI_PlayerViewer PlayerViewerCanvas;
    public Scr_UI_FinalScoreCanvas FinalScoreCanvas;
    public Scr_Camera_Follow NetworkPlayerCamera;
    public Scr_HealthPackManager HealthPackManager;

    [Header("Properties")]
    Scr_Player LocalPlayer; 
    Coroutine GetNetworkPlayerRoutine;
    bool GameOver;
    public int CurrentWave { get; private set; } = 0;

    [Header("FMOD")]
    public string FMODGameOver;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if(NakamaConnection.Instance.IsMultiplayer())
        {
            AttachToNakama();
            SpawnLocalPlayer();
        }
        else
        {
            SpawnLocalPlayer();
            NextWave();
        }
    }

    IEnumerator ResendRequestCharacter()
    {
        yield return new WaitForSeconds(1);
        SendData(OpCodes.RequestCharacter);
        GetNetworkPlayerRoutine = StartCoroutine(ResendRequestCharacter());
    }

    [ContextMenu("NextWave")]
    public void NextWave()
    {
        if (NakamaConnection.Instance.IsMultiplayer())
        {
            ReviveLocalPlayer();
        }

        if (!NakamaConnection.Instance.AmIHost())
        {
            return;
        }
        else
        {
            HealthPackManager.DecideHealthPackSpawn();
        }

        CurrentWave++;
        SendData(OpCodes.NextWave, MatchDataJSON.EncryptString(CurrentWave.ToString()));
        WaveCanvas.NextWave();
    }

    public void ReviveLocalPlayer()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Scr_Player Player = Players[i].GetComponent<Scr_Player>();

            if (NakamaConnection.Instance.IsLocalPlayer(Player.PlayerUsername)) //Found local player
            {
                if (Player.MyHealth.CurrentHealth <= 0) //Player is dead
                {
                    Player.transform.position = GeneratePlayerSpawnPoint();
                    Player.Revive();
                    break;
                }
            }
        }
    }

    public IEnumerator PlayerDisconnect()
    {
        if (GameOver)
        {
            yield break;
        }

        bool PlayerStillAlive = false;
        Transform LivePlayer = null;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].GetComponent<Scr_Health>().CurrentHealth > 0)
            {
                LivePlayer = Players[i].transform;
                PlayerStillAlive = true;
                break;
            }
        }

        if (!PlayerStillAlive)
        {
            GameOver = true;
            yield return new WaitForSeconds(1);
            WaveCanvas.GameOverAnimation();
            Scr_AudioManager.Instance.PlayOneShot2D(FMODGameOver);
        }
        else
        {
            Scr_WaveSpawner.Instance.UpdateTarget();
        }

    }

    public IEnumerator PlayerDeath(Scr_Player _DeadPlayer)
    {
        if(GameOver)
        {
            yield break;
        }

        bool PlayerStillAlive = false;
        Transform LivePlayer = null;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].GetComponent<Scr_Health>().CurrentHealth > 0)
            {
                LivePlayer = Players[i].transform;
                PlayerStillAlive = true;
                break;
            }
        }

        if (!PlayerStillAlive)
        {
            GameOver = true;
            yield return new WaitForSeconds(1);
            WaveCanvas.GameOverAnimation();
            Scr_AudioManager.Instance.PlayOneShot2D(FMODGameOver);
        }
        else
        {
            Scr_WaveSpawner.Instance.UpdateTarget();

            //Only swap camera focus if local player died
            //If its not local player that died, no need to swap camera.
            if (_DeadPlayer != LocalPlayer)
            {
                yield break;
            }

            yield return new WaitForSeconds(1);

            //If the dead player (The local player, as non local is filtered out) :
            //becomes alive after waiting for 1 second, no need to swap camera anymore.
            if(_DeadPlayer.MyHealth.CurrentHealth > 0)
            {
                yield break;
            }

            Camera.main.GetComponent<Scr_Camera_Follow>().AssignPlayerToFollow(LivePlayer);
        }
    }

    void SpawnLocalPlayer()
    {
        //Generate Random Spawn Point
        Vector2 RandomSpawn = GeneratePlayerSpawnPoint();

        //Spawn player
        GameObject SpawnedPlayer = Instantiate(LocalPlayerPrefab, RandomSpawn, Quaternion.identity);
        Scr_Player ThePlayer = SpawnedPlayer.GetComponent<Scr_Player>();

        if (NakamaConnection.Instance.IsMultiplayer())
        {
            //Set Player Stat Canvas
            Scr_UI_PlayerCanvas CanvasToAssign = NakamaConnection.Instance.AmIHost() ? Player1Canvas : Player2Canvas;
            
            //Set FinalScroe Canvas
            if(NakamaConnection.Instance.AmIHost())
            {
                Scr_UI_FinalScoreCanvas.Instance.Player1 = ThePlayer;
            }
            else
            {
                Scr_UI_FinalScoreCanvas.Instance.Player2 = ThePlayer;
            }

            ThePlayer.AssignPlayerCanvas(CanvasToAssign);
            ThePlayer.PlayerUsername = NakamaConnection.Instance.ConnectedMatch.Self.Username;

            if(CanvasToAssign == Player2Canvas)
            {
                CanvasToAssign.GetComponent<Canvas>().enabled = true;
            }

            CanvasToAssign.PlayerIDText.text += " (You)";
        }
        else
        {
            ThePlayer.AssignPlayerCanvas(Player1Canvas);
            Scr_UI_FinalScoreCanvas.Instance.Player1 = ThePlayer;
        }

        if(NakamaConnection.Instance.session != null)
        {
            ThePlayer.PlayerUsername = NakamaConnection.Instance.session.Username;
        }
        else
        {
            ThePlayer.PlayerUsername = "localhost";
        }


        if (Scr_WWW_SpriteDownloader.Instance != null && Scr_WWW_SpriteDownloader.Instance.PlayerSprites.ContainsKey("Local"))
        {
            ThePlayer.GetComponentInChildren<Scr_Animation_SpriteAnimator_Player>().SetDownloadedSprites(Scr_WWW_SpriteDownloader.Instance.PlayerSprites["Local"]);
        }

        //Assignment for local entities
        Scr_UI_SkillCanvas.Instance.AssignPlayer(ThePlayer);
        Camera.main.GetComponent<Scr_Camera_Follow>().AssignPlayerToFollow(ThePlayer.transform);
        LocalPlayer = ThePlayer;

        //Add to Player list
        Players.Add(ThePlayer);

        //Request for other player's local character
        if(NakamaConnection.Instance.IsMultiplayer())
        {
            SendData(OpCodes.RequestCharacter);
            GetNetworkPlayerRoutine = StartCoroutine(ResendRequestCharacter());
        }
    }

    void SpawnNetworkPlayer(Vector2 _Position, IUserPresence _ClientPresence)
    {
        GameObject SpawnedPlayer = Instantiate(NetworkPlayerPrefab, _Position, Quaternion.identity);

        Scr_Player ClientPlayer = SpawnedPlayer.GetComponent<Scr_Player>();
        Players.Add(ClientPlayer);

        if (NakamaConnection.Instance.AmIHost())
        {
            Scr_UI_FinalScoreCanvas.Instance.Player2 = ClientPlayer;
        }
        else
        {
            Scr_UI_FinalScoreCanvas.Instance.Player1 = ClientPlayer;
        }

        ClientPlayer.AssignPlayerCanvas(!NakamaConnection.Instance.AmIHost() ? Player1Canvas : Player2Canvas);
        ClientPlayer.PlayerUsername = _ClientPresence.Username;

        //Setting this network player's sprite
        Sprite[] NetworkSprite;
        if(Scr_WWW_SpriteDownloader.Instance.PlayerSprites.TryGetValue(_ClientPresence.Username, out NetworkSprite))
        {
            ClientPlayer.GetComponentInChildren<Scr_Animation_SpriteAnimator_Player>().SetDownloadedSprites(NetworkSprite);
        }

        Player2Canvas.GetComponent<Canvas>().enabled = true;
        NetworkPlayerCamera.AssignObjectToFollow(ClientPlayer.transform);
        NetworkPlayerCamera.PlayerToFollow = ClientPlayer.transform;
        NetworkPlayerCamera.gameObject.SetActive(true);
        PlayerViewerCanvas.SetNetworkPlayer(ClientPlayer);

        if (NakamaConnection.Instance.AmIHost() && NakamaConnection.Instance.RoomSize == Players.Count)
        {
            NextWave();
        }
    }

    Vector2 GeneratePlayerSpawnPoint()
    {
        Vector2 RandomSpawn = PlayerSpawnZone.bounds.center;
        RandomSpawn.x += Random.Range(-PlayerSpawnZone.bounds.extents.x, PlayerSpawnZone.bounds.extents.x);
        RandomSpawn.y += Random.Range(-PlayerSpawnZone.bounds.extents.y, PlayerSpawnZone.bounds.extents.y);
        return RandomSpawn;
    }

    /// <summary>
    /// Send Data across the network
    /// </summary>
    /// <param name="_Code"></param>
    /// <param name="_Params"></param>
    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        switch (_Code)
        {
            case OpCodes.CharacterSpawn:
            case OpCodes.NextWave:
                NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
                break;
            case OpCodes.RequestCharacter:
            case OpCodes.StartGame:
                NakamaConnection.Instance.RequestSendMatchState(_Code);
                break;
        }

    }

    public void ReceiveData(IMatchState _MatchState)
    {
        switch(_MatchState.OpCode)
        {
            case OpCodes.CharacterSpawn:
                Dictionary<string, string> ReceivedData = MatchDataJSON.DecryptStateToDictionary(_MatchState.State);
                Vector2 ReceivedPosition;
                ReceivedPosition.x = float.Parse(ReceivedData["Position X"]);
                ReceivedPosition.y = float.Parse(ReceivedData["Position Y"]);

                Scr_MainThreadDispatcher.Instance.Enqueue(() => 
                {
                    StopCoroutine(GetNetworkPlayerRoutine);
                    SpawnNetworkPlayer(ReceivedPosition, _MatchState.UserPresence);
                });
                break;
            case OpCodes.RequestCharacter:
                Scr_MainThreadDispatcher.Instance.Enqueue(() => 
                {
                    Scr_Player LocalPlayer = null;
                    for (int i = 0; i < Players.Count; i++)
                    {
                        if (NakamaConnection.Instance.IsLocalPlayer(Players[i].PlayerUsername))
                        {
                            LocalPlayer = Players[i];
                            break;
                        }
                    }

                    Vector2 LocalPlayerPosition = LocalPlayer.transform.position;
                    SendData(OpCodes.CharacterSpawn, MatchDataJSON.EncryptVelocityAndPosition(Vector2.zero, LocalPlayerPosition));
                });
                break;
            case OpCodes.StartGame:
                Scr_MainThreadDispatcher.Instance.Enqueue(() => 
                {
                    NextWave();
                });
                break;
            case OpCodes.NextWave:
                CurrentWave = int.Parse(MatchDataJSON.DecryptStateToString(_MatchState.State));
                Scr_MainThreadDispatcher.Instance.Enqueue(() => WaveCanvas.NextWave());
                break;
        }
    }

    //public void PlayerLoaded()
    //{
    //    if(NakamaConnection.Instance.IsMultiplayer()) //Multiplayer
    //    {
    //        PlayersLoaded++;

    //        if (NakamaConnection.Instance.AmIHost()) //Host decides when to start game
    //        {
    //            Debug.Log("Are we ready start? " + (PlayersLoaded == NakamaConnection.Instance.RoomSize));

    //            if (PlayersLoaded == NakamaConnection.Instance.RoomSize)
    //            {
    //                WaveCanvas.BlackCover.enabled = false;
    //                SendData(OpCodes.StartGame);
    //                NextWave();
    //            }
    //        }
    //    }
    //    else
    //    {
    //        WaveCanvas.BlackCover.enabled = false;
    //        NextWave();
    //    }
    //}

    public void AttachToNakama()
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }

    public void SwitchToSinglePlayer()
    {
        Players.RemoveAll(item => item == null);
        StartCoroutine(PlayerDisconnect());
        PlayerViewerCanvas.gameObject.SetActive(false);
        Scr_WaveSpawner.Instance.CheckCurrentWave(null);
        Scr_WaveSpawner.Instance.SwitchToSinglePlayer();
        Scr_UI_FinalScoreCanvas.Instance.Player1 = Players[0];
        NetworkPlayerCamera.gameObject.SetActive(false);

        if(Players[0].MyPlayerCanvas == Player2Canvas)
        {
            Player2Canvas.PingCanvas.gameObject.SetActive(false);
            Player2Canvas.PingCanvas.CloseUI();
        }
    }
}
