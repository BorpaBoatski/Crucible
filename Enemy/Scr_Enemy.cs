using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Nakama;

public class Scr_Enemy : MonoBehaviour, INetworkComponent 
{
    [Header("Developer")]
    public int Damage = 1;
    public float SlowDownDuration = 1;
    static bool DealDamage = true;
    public float DurationTillCanRetaliate = 5;
    static float MinDistanceToTeleport = 5;

    [Header("References")]
    public SpriteRenderer MyRenderer;
    public SpriteRenderer MyRendererWhite;
    public SpriteRenderer MyRendererYellow;
    public SpriteRenderer MyRendererRed;
    public Scr_Database_Enemy_Stats[] StatPresets;
    public SpriteRenderer ColorRenderer;
    public Enum_EnemyType MyType;

    [Header("Properties")]
    bool CanRetaliate = true;
    Rigidbody2D MyRigidbody;
    AIDestinationSetter AIDestination;
    public Transform Target { get; private set; }
    AILerp MyAILerp;
    public Enum_EnemyRarityType MyRarityType { get; private set; }
    public Scr_Health_Enemy MyHealth { get; private set; }
    public Scr_EnemyIdleSound IdleSoundScript { get; private set; }
    Vector2 LerpOrigin;

    public void SetCanRetaliate(bool _State)
    {
        CanRetaliate = _State;

        if (!_State)
        {
            StartCoroutine(RetaliateCooldown());
        }
    }

    private void Awake()
    {
        if(MyRigidbody != null)
        {
            Initialize();
        }
    }

    void Initialize()
    {
        MyRigidbody = GetComponent<Rigidbody2D>();
        AIDestination = GetComponent<AIDestinationSetter>();
        MyAILerp = GetComponent<AILerp>();
        IdleSoundScript = GetComponent<Scr_EnemyIdleSound>();

        if (IsNetworkReceiver)
        {
            MyAILerp.enabled = false;
        }

        MyHealth = GetComponent<Scr_Health_Enemy>();
        NetworkName = transform.name;
    }

    /// <summary>
    /// Enemy initialization
    /// </summary>
    /// <param name="_Type"></param>
    public void OnSpawn(Enum_EnemyRarityType _Type)
    {
        //Properties not initialized
        if(MyRigidbody == null)
        {
            Initialize();
        }

        ResetRenderer();

        MyRarityType = _Type;

        switch (_Type)
        {
            case Enum_EnemyRarityType.A:
                //MyRenderer.color = new Color(0.3f,0.3f,0.3f,1);
                MyRendererWhite.gameObject.SetActive(true);
                MyRenderer = MyRendererWhite;             
                break;
            case Enum_EnemyRarityType.B:
                //MyRenderer.color = Color.yellow;
                MyRendererYellow.gameObject.SetActive(true);
                MyRenderer = MyRendererYellow;
                break;
            case Enum_EnemyRarityType.C:
                //MyRenderer.color = Color.red;
                MyRendererRed.gameObject.SetActive(true);
                MyRenderer = MyRendererRed;
                break;
        }

        MyHealth.Initialize(StatPresets[(int)_Type].MaxHealth);
        MyHealth.MyAnimator = MyRenderer.GetComponent<Animator>();
        MyHealth.MySpriteAnimator = MyRenderer.GetComponent<Scr_Animation_SpriteAnimator>();
        MyAILerp.speed = StatPresets[(int)_Type].Speed;
        Damage = StatPresets[(int)_Type].Damage;
        gameObject.SetActive(true);
        CanRetaliate = true;
        SyncTimer = SyncRate;
        LerpTimer = 1;
        FindTargetByDistance();
    }

    void ResetRenderer()
    {
        MyRendererWhite.gameObject.SetActive(false);
        MyRendererYellow.gameObject.SetActive(false);
        MyRendererRed.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if(IsNetworkReceiver)
        {
            MyRigidbody.velocity = NetworkVelocity;
            UpdateFacingDirection(MyRigidbody.velocity);

            if (LerpTimer < 1)
            {
                if(Vector2.Distance(LerpOrigin, LerpPosition) > MinDistanceToTeleport)
                {
                    //Debug.Log(transform.name + " teleported\n" + transform.position + " " + LerpPosition);
                    transform.position = LerpPosition;
                    LerpTimer = 1;
                    return;
                }

                LerpTimer += Time.deltaTime;
                transform.position = Vector2.Lerp(LerpOrigin, LerpPosition, LerpTimer / SyncRate);
            }
        }
        else
        {
            UpdateFacingDirection(MyAILerp.velocity);
        }
    }

    void Update()
    {
        if(!IsNetworkReceiver && NakamaConnection.Instance.IsMultiplayer())
        {
            if(SyncTimer <= 0)
            {
                SyncTimer = SyncRate;
                //SendData(OpCodes.EnemyVelocityPosition);
                SendSync();
            }
            else
            {
                SyncTimer -= Time.deltaTime;
            }
        }
    }

    void FindTargetByDistance()
    {
        float ShortestDistanceToPlayer = 0;
        GameObject PossibleTarget = null;

        for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
        {
            try
            {
                float TestDistanceToPlayer = Vector2.Distance(Scr_GameManager.Instance.Players[i].transform.position, transform.position);

                if ((ShortestDistanceToPlayer == 0 || TestDistanceToPlayer < ShortestDistanceToPlayer) && Scr_GameManager.Instance.Players[i].GetComponent<Scr_Health>().CurrentHealth > 0)
                {
                    ShortestDistanceToPlayer = TestDistanceToPlayer;
                    PossibleTarget = Scr_GameManager.Instance.Players[i].gameObject;
                }
            }
            catch(MissingReferenceException)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        UpdateTarget(PossibleTarget.transform);
    }


    void UpdateFacingDirection(Vector2 _Velocity)
    {
        if(MyHealth.CurrentHealth <= 0)
        {
            return;
        }

        if (_Velocity.x > 0)
        {
            MyRenderer.flipX = false;
            ColorRenderer.flipX = false;
        }
        else if (_Velocity.x < 0)
        {
            MyRenderer.flipX = true;
            ColorRenderer.flipX = true;
        }
    }

    /// <summary>
    /// Spawn EXP on monster's location. Happens upon death
    /// </summary>
    public void SpawnEXP()
    {
        Scr_EXPManager.Instance.SpawnEXP(transform.position, (Enum_EXPType)((int)MyRarityType));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Scr_Player HitPlayer = collision.gameObject.GetComponent<Scr_Player>();

            //Collision with a player that isn't the local player
            if (!NakamaConnection.Instance.IsLocalPlayer(HitPlayer.PlayerUsername))
            {
                return;
            }

            HitPlayer.MyHealth.TakeDamage(DealDamage ? StatPresets[(int)MyRarityType].Damage : 0, gameObject);
            StartCoroutine(DamagedSlowDown());
        }
    }

    /// <summary>
    /// To slow down the enemy after they land a hit
    /// </summary>
    /// <returns></returns>
    public IEnumerator DamagedSlowDown()
    {
        MyAILerp.speed = StatPresets[(int)MyRarityType].Speed/2;
        yield return new WaitForSeconds(SlowDownDuration);
        MyAILerp.speed = StatPresets[(int)MyRarityType].Speed;
    }

    /// <summary>
    /// Change chase target
    /// </summary>
    /// <param name="_NewTarget"></param>
    public void UpdateTarget(Transform _NewTarget)
    {
        //AI logic will be performed on host side
        if(!NakamaConnection.Instance.AmIHost())
        {
            return;
        }

        Target = _NewTarget;
        AIDestination.target = _NewTarget;
    }

    /// <summary>
    /// Find a new target
    /// </summary>
    public void FindNewTarget()
    {
        float Distance = -1;
        Transform PlayerToChase = null;

        for (int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
        {
            if (Scr_GameManager.Instance.Players[i].GetComponent<Scr_Health>().CurrentHealth <= 0)
            {
                continue;
            }

            float CalculatedDistance = Vector2.Distance(transform.position, Scr_GameManager.Instance.Players[i].transform.position);

            if (CalculatedDistance <= Distance || Distance == -1)
            {
                PlayerToChase = Scr_GameManager.Instance.Players[i].transform;
            }
        }

        UpdateTarget(PlayerToChase);
    }

    public void EnemyDeath()
    {
        IdleSoundScript.StopIdleSound();
        SpawnEXP();
        LerpTimer = 1;
        AIDestination.target = null;
        MyRigidbody.velocity = Vector2.zero;
    }

    public void Retaliate(GameObject _Instigator)
    {
        if(!NakamaConnection.Instance.AmIHost() || !CanRetaliate)
        {
            return;
        }

        //Debug.Log(_Instigator);
        //Debug.Log(AIDestination.target);

        if(AIDestination.target == null || Target == null)
        {
            return;
        }

        if (Vector2.Distance(_Instigator.transform.position, transform.position) < Vector2.Distance(AIDestination.target.position, transform.position) || Target.GetComponent<Scr_Health>().CurrentHealth == 0)
        {
            RaycastHit2D[] NearbyEnemies = Physics2D.CircleCastAll(transform.position, 5, Vector2.zero, 0, Scr_WaveSpawner.Instance.GetEnemyLayerMask());

            foreach(RaycastHit2D Hit2D in NearbyEnemies)
            {
                Scr_Enemy Enemy = Hit2D.rigidbody.GetComponent<Scr_Enemy>();
                Enemy.UpdateTarget(_Instigator.transform);
                Enemy.SetCanRetaliate(false);
            }
        }
    }

    IEnumerator RetaliateCooldown()
    {
        yield return new WaitForSeconds(DurationTillCanRetaliate);
        SetCanRetaliate(true);
    }

    #region Networking

    [Header("Networking")]
    static float SyncRate = .1f;

    bool IsNetworkReceiver;
    public void SetNetworkReceiver(bool _State)
    {
        IsNetworkReceiver = _State;

        if (_State)
        {
            AttachToNakama();
        }
        else
        {
            //Check if this Enemy has been initialized before
            if(MyAILerp != null)
            {
                MyAILerp.enabled = true;
            }
        }
    }

    float SyncTimer;
    Vector2 LerpPosition;
    float LerpTimer = 1;
    public string NetworkName { get; private set;  }
    Vector2 NetworkVelocity;

    void SendSync()
    {
        SendData(OpCodes.EnemyState, MatchDataJSON.EncryptEnemyState(NetworkName, MyAILerp.velocity, transform.position, MyHealth.CurrentHealth, MyRarityType, MyType));
    }

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer() || IsNetworkReceiver)
        {
            return;
        }

        switch(_Code)
        {
            //case OpCodes.EnemyVelocityPosition:
            //    //Debug.Log("Sending Velocity: " + MyAILerp.velocity);
            //    NakamaConnection.Instance.RequestSendMatchState(MatchDataJSON.EncryptEnemyVelocityAndPosition(MyAILerp.velocity, transform.position, transform.name), _Code);
            //    break;
            case OpCodes.EnemyState:
                NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
                break;
        }
    }

    public void ReceiveData(IMatchState _MatchState)
    {
        if(!IsNetworkReceiver)
        {
            return;
        }

        switch(_MatchState.OpCode)
        {
            //case OpCodes.EnemyVelocityPosition:
            //    Dictionary<string, string> EnemyData = MatchDataJSON.DecryptStateToDictionary(_MatchState.State);

            //    if (EnemyData["Name"] == NetworkName)
            //    {
            //        Vector2 Velocity = new Vector2(float.Parse(EnemyData["Velocity X"]), float.Parse(EnemyData["Velocity Y"]));
            //        NetworkVelocity = Velocity;

            //        if(Velocity == Vector2.zero && MyHealth.CurrentHealth <= 0)
            //        {
            //            LerpTimer = .99f;
            //        }

            //        Vector2 ReceivedLerpPosition = new Vector2(float.Parse(EnemyData["Position X"]), float.Parse(EnemyData["Position Y"]));

            //        if (ReceivedLerpPosition != LerpPosition)
            //        {
            //            LerpPosition = ReceivedLerpPosition;
            //            LerpTimer = 0;
            //        }
            //    }
            //    break;
            case OpCodes.EnemyState:
                Dictionary<string, string> EnemyState = MatchDataJSON.DecryptStateToDictionary(_MatchState.State);

                if(EnemyState["Name"].Equals(NetworkName))
                {
                    Scr_MainThreadDispatcher.Instance.Enqueue(() => Sync(EnemyState));
                }

                break;
        }
    }

    void Sync(Dictionary<string, string> _EnemyState)
    {
        //Movement syncing
        Vector2 Velocity = new Vector2(float.Parse(_EnemyState["Velocity X"]), float.Parse(_EnemyState["Velocity Y"]));
        NetworkVelocity = Velocity;

        if (int.Parse(_EnemyState["Health"]) <= 0)
        {
            LerpTimer = .99f;
        }

        Vector2 ReceivedLerpPosition = new Vector2(float.Parse(_EnemyState["Position X"]), float.Parse(_EnemyState["Position Y"]));

        if (ReceivedLerpPosition != LerpPosition)
        {
            LerpOrigin = transform.position;
            LerpPosition = ReceivedLerpPosition;
            LerpTimer = 0;
        }

        //Health sync
        MyHealth.Sync(int.Parse(_EnemyState["Health"]));
    }

    public void AttachToNakama()
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }

    #endregion

}
