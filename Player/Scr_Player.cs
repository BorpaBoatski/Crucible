using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nakama;

public class Scr_Player : MonoBehaviour, INetworkComponent
{
    [Header("Developer")]
    public string FMODInvulPower;
    static int EXPStep = 200;
    static float ExpStepMultiply = 1.5f;
    static int BaseEXP = 100;

    [Header("Testing")]
    [Range(5, 1000)]
    public int TestingEXPGain = 5;
    [Range(5, 100)]
    public int TestingScoreGain = 5;

    [Header("Networking Developer")]
    public float SyncRate = 0.05f;

    [Header("Networking Properties")]
    float NextSyncTimer = 0;
    public string PlayerUsername;

    [Header("References")]
    public SpriteRenderer MyRenderer;
    public SpriteRenderer ColorRenderer;
    public ParticleSystem HealthPowerUpParticle;

    [Header("Properties")]
    int EarnedScore = 0;
    public int CurrentLevel { get; private set; } = 1;
    int MaxLevel = 0;
    int CurrentEXP = 0;
    int RequiredEXP = 10;
    public Scr_Player_Movement MyMovement { get; private set; }
    public Scr_Player_Shooting MyShooting { get; private set; }
    public Scr_Health_Player MyHealth { get; private set; }
    public int UnusedSkill { get; private set; } = 0;
    public Scr_UI_PlayerCanvas MyPlayerCanvas { get; private set; }
    Canvas MyResultCanvas;

    [Header("FMOD")]
    public string FMODLevelUp;
    public string FMODSkillUp;

    private void Awake()
    {
        MyMovement = GetComponent<Scr_Player_Movement>();
        MyShooting = GetComponent<Scr_Player_Shooting>();
        MyHealth = GetComponent<Scr_Health_Player>();
        MyHealth.HealthChangeDelegate += MessageUIUpdateHealth;

        // All max skill minus one since all skill starts with Level 1.
        // if maxskill is 6, skill start with 1, they have 5 levels to increase. 
        MaxLevel += MyMovement.MaxLevel - 1;
        MaxLevel += MyShooting.MaxDamageLevel - 1;
        MaxLevel += MyShooting.MaxFireRateLevel - 1;
        MaxLevel += MyHealth.MaxHealthLevel - 1;
        
        // Finally, the player starts at level 1.
        // We need  to add one to the max level.
        // for example max level 20, so they should level up 20 times, but 
        // player starts at level 1, means they only level up 19, if we dont +1.
        MaxLevel += 1;
        RequiredEXP = BaseEXP;
    }

    private void Start()
    {
        AttachToNakama();   
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.B))
    //    {
    //        TestGainEXP();
    //    }

    //    if (Input.GetKeyDown(KeyCode.V))
    //    {
    //        TestGainScore();
    //    }
    //}

    /// <summary>
    /// Send new state sync after calculations from FixedUpdate
    /// </summary>
    private void Update()
    {
        NetworkSync();
    }

    void NetworkSync()
    {
        if (NakamaConnection.Instance.IsMultiplayer() && NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            if (NextSyncTimer <= 0)
            {
                SendSync();
            }
            else
            {
                NextSyncTimer -= Time.fixedDeltaTime;
            }
        }
    }

    public void MessageUIUpdateHealth(int _Amount)
    {
        MyPlayerCanvas.UpdateHealth(MyHealth.CurrentHealth, MyHealth.MaxHealth, _Amount);
    }

    void MessageUIUpdateScore()
    {
        MyPlayerCanvas.UpdateScore(EarnedScore);
    }

    void MessageUIUpdateEXP()
    {
        bool IsTurningMaxLevel = CurrentLevel >= MaxLevel;
        MyPlayerCanvas.UpdateEXP(CurrentEXP, RequiredEXP, IsTurningMaxLevel);
    }

    public void EarnScore(int _EarnedScore)
    {
        EarnedScore += _EarnedScore;
        MessageUIUpdateScore();
        //SendData(OpCodes.GainScore, _EarnedScore.ToString());
    }

    public int GetScore()
    {
        return EarnedScore;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch(collision.tag)
        {
            case "EXP": //Player moves into EXP
                Scr_Drops_EXP EXP = collision.GetComponent<Scr_Drops_EXP>();

                if(NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
                {
                    GainEXP(EXP.GetEXP());
                    //SendData(OpCodes.GainEXP, EXP.name);
                }

                EXP.PickUp();
                break;
            case "PowerUp": //Player moves into EXP
                Scr_PowerUp PowerUp = collision.GetComponent<Scr_PowerUp>();
                PowerUp.PickUp(this);
                break;
        }
    }

    void GainEXP(int _EXP)
    {
        if(CurrentLevel >= MaxLevel)
        {
            EarnScore(_EXP);
            return;
        }

        CurrentEXP += _EXP;

        if(CurrentEXP >= RequiredEXP)
        {
            CurrentLevel++;
            CurrentEXP = 0;
           // RequiredEXP += EXPStep;
            RequiredEXP = (int)(RequiredEXP * ExpStepMultiply);

            if (NakamaConnection.Instance.IsLocalPlayer(PlayerUsername)) //Leveling should only affect local player
            {
                ModifyUnusedSkill(1);
                Scr_UI_SkillCanvas.Instance.OpenUI();
                Scr_AudioManager.Instance.PlayOneShot2D(FMODLevelUp);
            }
        }

        MessageUIUpdateEXP();
    }

    public void AddSkill(int _Skill)
    {
        ModifyUnusedSkill(-1);

        switch(_Skill)
        {
            case 0:
                MyHealth.LevelUpMaxHealth();
                break;
            case 1:
                MyMovement.LevelUpSpeed();
                break;
            case 2:
                MyShooting.LevelUpFireRate();
                break;
            case 3:
                MyShooting.LevelUpDamage();
                break;
        }

        Scr_UI_SkillCanvas.Instance.UpdateSkillsText();
        Scr_AudioManager.Instance.PlayOneShot2D(FMODSkillUp);
    }

    public void ModifyUnusedSkill(int _Amount)
    {
        UnusedSkill += _Amount;
        Scr_UI_SkillCanvas.Instance.UpdateUnusedText();
    }

    [ContextMenu("Heal")]
    public void HealthPowerUp()
    {
        MyHealth.ModifyCurrentHealth(MyHealth.MaxHealth);
        HealthPowerUpParticle.Play();

        //Not local player
        if (!NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            return;
        }

        Scr_UI_OnscreenEffect.Instance.PlayHealVignette();
    }

    [ContextMenu("Invul")]
    public void InvulPowerUp()
    {
        MyHealth.SetInvulPower(true);
    }

    [ContextMenu("SpeedUp")]
    public void SpeedBoostPowerUp()
    {
        MyMovement.SetSpeedBoost(true);
    }


    [ContextMenu("Pierce")]
    public void PiercePowerUp()
    {
        MyShooting.SetPierce(true);
    }
    public void Revive()
    {
        CurrentEXP = 0;
        //RequiredEXP = BaseEXP;
        MyHealth.Revive();
        //MyShooting.Revive();
        //MyMovement.Revive();
        Scr_UI_SkillCanvas.Instance.UpdateSkillsText();
        //CurrentLevel = 1;
        //ModifyUnusedSkill(-UnusedSkill);
        MyPlayerCanvas.ResetSlider();

        if (NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            //SendData(OpCodes.PlayerRevive, MatchDataJSON.EncryptVelocityAndPosition(Vector2.zero, transform.position));
            Camera.main.GetComponent<Scr_Camera_Follow>().AssignPlayerToFollow(transform);
        }
    }

    public void AssignPlayerCanvas(Scr_UI_PlayerCanvas _Canvas)
    {
        MyPlayerCanvas = _Canvas;
        _Canvas.SetAssignedPlayer(this);
    }

    #region Networking

    /// <summary>
    /// Send data over the network to sync the position, velocity, and facing direction of the player
    /// </summary>
    void SendSync()
    {
        //Disabled movement would mean the player is dead
        //if (!MyMovement.enabled)
        //{
        //    return;
        //}

        //MyMovement.SendData();
        //MyShooting.SendData();
        if(!NakamaConnection.Instance.IsMultiplayer() || !NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            return;
        }

        SendData(OpCodes.PlayerState, MatchDataJSON.EncryptPlayerState(MyMovement.MyRigidbody.velocity, transform.position, MyShooting.PropAimDirection, MyHealth.CurrentHealth, CurrentLevel, CurrentEXP, EarnedScore));
    }

    //void NetworkEarnScore(int _ScoreEarned)
    //{
    //    EarnedScore += _ScoreEarned;
    //    MessageUIUpdateScore();
    //}

    //void NetworkEarnEXP(string _EXPName)
    //{
    //    Scr_Drops_EXP PickedEXP = Scr_EXPManager.Instance.GetEXP(_EXPName);
    //    GainEXP(PickedEXP.GetEXP());
    //    PickedEXP.PickUp();
    //}

    public void ReceiveData(IMatchState _MatchState)
    {
        if(NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            //Debug.Log("Not player");
            return;
        }

        switch(_MatchState.OpCode)
        {
            //case OpCodes.GainEXP:
            //    Scr_MainThreadDispatcher.Instance.Enqueue(() => NetworkEarnEXP(MatchDataJSON.DecryptStateToString(_MatchState.State)));
            //    break;
            //case OpCodes.GainScore:
            //    Scr_MainThreadDispatcher.Instance.Enqueue(() => NetworkEarnScore(int.Parse(MatchDataJSON.DecryptStateToString(_MatchState.State))));
            //    break;
            //case OpCodes.PlayerRevive:
            //    if(PlayerUsername == _MatchState.UserPresence.Username)
            //    {
            //        Dictionary<string, string> ReviveData = MatchDataJSON.DecryptStateToDictionary(_MatchState.State);
            //        Vector2 SpawnPosition;
            //        SpawnPosition.x = float.Parse(ReviveData["Position X"]);
            //        SpawnPosition.y = float.Parse(ReviveData["Position Y"]);
            //        Scr_MainThreadDispatcher.Instance.Enqueue(() => 
            //        {
            //            transform.position = SpawnPosition;
            //            Revive();
            //        });
            //    }
            //    break;
            case OpCodes.PlayerState:
                Scr_MainThreadDispatcher.Instance.Enqueue(() => UpdatePlayerState(MatchDataJSON.DecryptStateToDictionary(_MatchState.State)));
                break;
        }
    }

    void UpdatePlayerState(Dictionary<string, string> _Data)
    {
        //Movement sync
        Vector2 Position = new Vector2(float.Parse(_Data["Position X"]), float.Parse(_Data["Position Y"]));
        Vector2 Velocity = new Vector2(float.Parse(_Data["Velocity X"]), float.Parse(_Data["Velocity Y"]));
        MyMovement.Sync(Position, Velocity);

        //Aiming sync
        Vector2 AimDirection = new Vector2(float.Parse(_Data["AimDirection X"]), float.Parse(_Data["AimDirection Y"]));
        MyShooting.Sync(AimDirection);

        //Health sync
        MyHealth.Sync(int.Parse(_Data["Health"]));

        //Score, Level and EXP sync
        Sync(int.Parse(_Data["EXP"]), int.Parse(_Data["Level"]), int.Parse(_Data["Score"]));
    }

    void Sync(int _EXP, int _Level, int _Score)
    {
        if(_Score - EarnedScore != 0)
        {
            EarnScore(_Score - EarnedScore);
        }

        if(_Level - CurrentLevel != 0 || _EXP - CurrentEXP != 0)
        {
            if(_EXP - CurrentEXP == 0)
            {
                GainEXP(RequiredEXP - CurrentEXP);
            }
            else
            {
                GainEXP(_EXP - CurrentEXP);
            }
        }

    }

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer() || !NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            return;
        }

        switch(_Code)
        {
            case OpCodes.PlayerState:
                NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
                break;
        }

        //switch (_Code)
        //{
        //    case OpCodes.GainEXP:
        //    case OpCodes.GainScore:
        //    case OpCodes.PlayerRevive:
        //        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
        //        break;
        //    default:
        //        MyMovement.SendData();
        //        MyShooting.SendData();
        //        break;
        //}
    }

    public void AttachToNakama()
    {
        //MyUserPresence == ConnectedMatch.Self means that this entity is suppose to represent the local player. The local player should not be receiving any data. Only sending
        if (!NakamaConnection.Instance.IsMultiplayer() || NakamaConnection.Instance.IsLocalPlayer(PlayerUsername))
        {
            return;
        }

        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }

    public void Disconnect()
    {
        MyPlayerCanvas.CloseUI();
        Destroy(MyShooting.ProjectilePool.gameObject);
        Destroy(gameObject);
        return;
    }

    #endregion

    

    #region TestingShortcuts

    [ContextMenu("Level Up Speed")]
    void LevelUpSpeed()
    {
        MyMovement.LevelUpSpeed();
    }

    [ContextMenu("Level Up Health")]
    void LevelUpHealth()
    {
        MyHealth.LevelUpMaxHealth();
    }

    [ContextMenu("Gain EXP")]
    void TestGainEXP()
    {
        GainEXP(TestingEXPGain);
    }

    [ContextMenu("Gain Score")]
    void TestGainScore()
    {
        EarnScore(TestingScoreGain);
    }

    [ContextMenu("Uber")]
    void Uber()
    {
        for(int i = 0; i < 10; i++)
        {
            MyMovement.LevelUpSpeed();
            MyShooting.LevelUpDamage();
            MyShooting.LevelUpFireRate();
        }
    }

    #endregion
}
