using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Nakama;

public class Scr_Player_Shooting : MonoBehaviour, INetworkComponent
{
    [Header("Developer")]
    public float InitialDelayBetweenShots = 1;
    public float DelayDecreasePerLevel = 0.1f;
    public int InitialDamage = 1;
    public int DamageIncreasePerLevel = 1;
    public int MaxFireRateLevel = 6;
    public int MaxDamageLevel = 6;
    public float PiercingPowerDuration;

    [Header("Spawning")]
    public GameObject AxePrefab;

    [Header("Reference")]
    public Transform AimPivot;
    public Transform ProjectilePool;
    public Transform ProjectileSpawn;
    public ParticleSystem SpeedBoostEffect;

    #region Properties

    Vector3 AimDirection;
    public Vector3 PropAimDirection
    {
        get
        {
            return AimDirection;
        }
    }

    public int FireRateLevel { get; private set; } = 1;
    float DelayTimer;
    public int DamageLevel { get; private set; } = 1;
    bool IsPiercing;
    float PierceDuration;
    Scr_Player MasterController;
    float CurrentDelayBetweenShots;
    int CurrentDamage;

    #endregion

    private void Awake()
    {
        MasterController = GetComponent<Scr_Player>();
        SpeedEffectMain = SpeedBoostEffect.main;
        CurrentDelayBetweenShots = InitialDelayBetweenShots;
        CurrentDamage = InitialDamage;
    }

    private void Start()
    {
        AttachToNakama();
        ProjectilePool.transform.SetParent(null);
    }

    private void FixedUpdate()
    {
        if(!IsNetworkReceiver)
        {
            UpdateAimDirection();

            if (DelayTimer <= 0)
            {
                if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Shoot();
                }
            }
            else
            {
                DelayTimer -= Time.deltaTime;
            }
        }

        if (PierceDuration > 0)
        {
            PierceDuration -= Time.fixedDeltaTime;

            if (PierceDuration <= 0)
            {
                if (IsPiercing)
                {
                    SetPierce(false);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if(IsNetworkReceiver)
        {
            UpdateAimPivot();
            UpdateCharacterFacing(AimDirection.x < 0);
        }
    }

    /// <summary>
    /// Calculate the direction the player is aiming at based on mouse position
    /// </summary>
    void UpdateAimDirection()
    {
        AimDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        AimDirection = AimDirection - transform.position;
        AimDirection.z = 0;
        UpdateAimPivot();
        UpdateCharacterFacing(AimDirection.x < 0);
    }

    /// <summary>
    /// Updates the arrow that shows the shooting direction of the player
    /// </summary>
    void UpdateAimPivot()
    {
        AimPivot.transform.up = AimDirection;
    }

    /// <summary>
    /// Handles the looking direction of the player sprite
    /// </summary>
    /// <param name="_FlipX"></param>
    void UpdateCharacterFacing(bool _FlipX)
    {
        MasterController.MyRenderer.flipX = _FlipX;
        MasterController.ColorRenderer.flipX = _FlipX;
        SpeedEffectMain.startRotationY = _FlipX ?  180 * Mathf.Deg2Rad : 0;
    }

    /// <summary>
    /// Shoot Request
    /// </summary>
    void Shoot()
    {
        DelayTimer = Mathf.Clamp(CurrentDelayBetweenShots, 0.1f, CurrentDelayBetweenShots);
        ActivateProjectile();
    }

    /// <summary>
    /// Find or create a projectile and shoot it
    /// </summary>
    void ActivateProjectile()
    {
        Scr_Player_EctoProjectile Projectile = null;

        for(int i = 0; i < ProjectilePool.childCount; i++)
        {
            if(ProjectilePool.GetChild(i).gameObject.activeSelf)
            {
                continue;
            }

            Projectile = ProjectilePool.GetChild(i).GetComponent<Scr_Player_EctoProjectile>();
            break;
        }

        if(Projectile == null)
        {
            Projectile = Instantiate(AxePrefab, AimPivot.transform.position, Quaternion.identity, ProjectilePool).GetComponent<Scr_Player_EctoProjectile>();
            Projectile.transform.name = AxePrefab.name + " (" + (ProjectilePool.childCount - 1).ToString() + ")";
            Projectile.Owner = MasterController;
        }

        Projectile.ShootThisProjectile(AimDirection, CurrentDamage, ProjectileSpawn, MasterController.MyRenderer.flipX, IsPiercing);
        SendData(OpCodes.Shoot, MatchDataJSON.EncryptShoot(Projectile.name, AimDirection, IsPiercing, CurrentDamage));
    }

    /// <summary>
    /// Find or create a projectile with specific name and shoot it
    /// </summary>
    void ActivateProjectile(string _AxeName, bool IsPierce)
    {
        Scr_Player_EctoProjectile Projectile = null;

        for (int i = 0; i < ProjectilePool.childCount; i++)
        {
            if (ProjectilePool.GetChild(i).name == _AxeName)
            {
                Projectile = ProjectilePool.GetChild(i).GetComponent<Scr_Player_EctoProjectile>();
                break;
            }
        }

        if (Projectile == null)
        {
            Projectile = Instantiate(AxePrefab, AimPivot.transform.position, Quaternion.identity, ProjectilePool).GetComponent<Scr_Player_EctoProjectile>();
            Projectile.transform.name = _AxeName;
            Projectile.Owner = MasterController;
        }

        Projectile.ShootThisProjectile(AimDirection, CurrentDamage, ProjectileSpawn, MasterController.MyRenderer.flipX, IsPierce);
    }


    public void SetPierce(bool _State)
    {
        IsPiercing = _State;
        
        if(_State)
        {
            PierceDuration = PiercingPowerDuration;
        }
        else
        {
            PierceDuration = 0;
        }
    }

    /// <summary>
    /// Player put points into FireRate
    /// </summary>
    public void LevelUpFireRate()
    {
        FireRateLevel++;
        CurrentDelayBetweenShots -= DelayDecreasePerLevel;
    }

    /// <summary>
    /// Player put points into Damage
    /// </summary>
    public void LevelUpDamage()
    {
        CurrentDamage += DamageIncreasePerLevel;
        DamageLevel++;
    }

    /// <summary>
    /// Reset points put into Damage and FireRate
    /// </summary>
    public void Revive()
    {
        FireRateLevel = 1;
        CurrentDamage = InitialDamage;
        CurrentDelayBetweenShots = InitialDelayBetweenShots;
        DamageLevel = 1;
    }

    #region Networking

    [Header("Network Properties")]
    ParticleSystem.MainModule SpeedEffectMain;
    bool IsNetworkReceiver = false;

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer() || IsNetworkReceiver)
        {
            return;
        }

        switch (_Code)
        {
            case OpCodes.Shoot:
                NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
                break;
        }
    }

    public void ReceiveData(IMatchState _State)
    {
        switch (_State.OpCode)
        {
            case OpCodes.Shoot:
                Dictionary<string, string> ReceivedData = MatchDataJSON.DecryptStateToDictionary(_State.State);
                AimDirection.x = float.Parse(ReceivedData["AimDirection X"]);
                AimDirection.y = float.Parse(ReceivedData["AimDirection Y"]);
                bool IsPierce = bool.Parse(ReceivedData["IsPiercing"]);
                CurrentDamage = int.Parse(ReceivedData["Damage"]);
                Scr_MainThreadDispatcher.Instance.Enqueue(() => ActivateProjectile(ReceivedData["AxeName"], IsPierce));
                break;
        }
    }

    public void AttachToNakama()
    {
        if(!NakamaConnection.Instance.IsMultiplayer() || NakamaConnection.Instance.IsLocalPlayer(MasterController.PlayerUsername))
        {
            return;
        }

        IsNetworkReceiver = true;
        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }

    public void Sync(Vector2 _AimDirection)
    {
        AimDirection = _AimDirection;
    }

    #endregion
}
