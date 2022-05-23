using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

public class Scr_Player_Movement : MonoBehaviour//, INetworkComponent
{
    [Header("Developer")]
    public float BaseSpeed = 2;
    public float SpeedIncreasePerLevel = 1;
    public int MaxLevel = 6;
    public float SpeedBoostDuration = 5;
    public float SpeedBoostSpeedIncrease = 2;

    [Header("References")]
    public ParticleSystem SpeedBoostParticle;

    [Header("Properties")]
    Vector2 Movement;
    public Rigidbody2D MyRigidbody { get; private set; }
    public int Level { get; private set; } = 1;
    bool SpeedBoost;
    float SpeedBoostTimer;
    Scr_Player MasterController;
    float Speed;

    [Header("Network Properties")]
    float LerpTime = 1;
    Vector2 LerpPosition;
    float MaxDistanceToTeleport = 5;
    Vector2 LerpOrigin;

    public bool IsReceiver { get; private set; } = false;

    private void Awake()
    {
        MyRigidbody = GetComponent<Rigidbody2D>();
        MasterController = GetComponent<Scr_Player>();
        Speed = BaseSpeed;
    }

    private void Start()
    {
        if(NakamaConnection.Instance.IsMultiplayer() && !NakamaConnection.Instance.IsLocalPlayer(MasterController.PlayerUsername))
        {
            IsReceiver = true;
        }
    }

    private void Update()
    {
        if(!IsReceiver)
        {
            LocalInput();
            Movement.Normalize();
            MyRigidbody.velocity = Movement * (Speed + (SpeedBoost ? SpeedBoostSpeedIncrease : 0));
        }
        else
        {
            MyRigidbody.velocity = Movement;

            if(LerpTime < 1)
            {
                if(Vector2.Distance(LerpOrigin, LerpPosition) > MaxDistanceToTeleport)
                {
                    LerpTime = 1;
                    transform.position = LerpPosition;
                    return;
                }

                LerpTime += Time.deltaTime;
                transform.position = Vector2.Lerp(LerpOrigin, LerpPosition, LerpTime / MasterController.SyncRate);
            }
        }


        if (SpeedBoostTimer > 0)
        {
            SpeedBoostTimer -= Time.deltaTime;

            if(SpeedBoostTimer <= 0)
            {
                SetSpeedBoost(false);
            }
        }
    }

    /// <summary>
    /// Update Movement based on local inputs
    /// </summary>
    void LocalInput()
    {
        Movement.y = Input.GetAxisRaw("Vertical");
        Movement.x = Input.GetAxisRaw("Horizontal");
    }

    /// <summary>
    /// Player puts points in Speed
    /// </summary>
    public void LevelUpSpeed()
    {
        Speed += SpeedIncreasePerLevel;
        Level++; 
    }

    /// <summary>
    /// Reset points on player Revive
    /// </summary>
    public void Revive()
    {
        Speed = BaseSpeed;
        Level = 1;
    }

    public void SetSpeedBoost(bool _State)
    {
        SpeedBoost = _State;

        if (_State)
        {
            SpeedBoostTimer = SpeedBoostDuration;
            SpeedBoostParticle.Play();
            Scr_UI_OnscreenEffect.Instance.ToggleSpeedLineParticleSystem(NakamaConnection.Instance.IsLocalPlayer(MasterController.PlayerUsername));
        }
        else
        {
            SpeedBoostTimer = 0;
            SpeedBoostParticle.Stop();
            Scr_UI_OnscreenEffect.Instance.ToggleSpeedLineParticleSystem(false);
        }
    }

    public void Death()
    {
        MyRigidbody.velocity = Vector2.zero;
        SetSpeedBoost(false);
    }

    #region Networking

    public void Sync(Vector2 _Position, Vector2 _Velocity)
    {
        Movement = _Velocity;

        if(_Position != LerpPosition)
        {
            LerpOrigin = transform.position;
            LerpTime = 0;
            LerpPosition = _Position;
        }
    }

    #endregion
}
