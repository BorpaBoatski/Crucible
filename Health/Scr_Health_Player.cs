using Nakama;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class Scr_Health_Player : Scr_Health
{
    [Header("Player Health Developer")]
    public float InvulPowerDuration;
    [Range(0, 1)]
    public float InvulStopWarning;
    public Color InvulTint = Color.yellow;
    public int InvulWarningBlinkAmount = 3;
    public int MaxHealthLevel = 6;
    public int BaseMaxHealth = 3;

    [Header("Player Health References")]
    public Light2D[] PlayerLight;

    [Header("Properties")]
    bool InvulPower;
    float InvulTimer;
    Scr_Player Master;
    Coroutine InvulWarningCoroutine;
    float InvulWarningDuration;
    public int HealthLevel { get; private set; } = 1;

    protected override void Awake()
    {
        base.Awake();
        Master = GetComponent<Scr_Player>();
        InvulWarningDuration = InvulPowerDuration * InvulStopWarning;
    }

    protected override void Start()
    {
        base.Start();
        Initialize(BaseMaxHealth);
    }

    private void FixedUpdate()
    {
        if(InvulTimer > 0)
        {
            InvulTimer -= Time.fixedDeltaTime;

            if(InvulTimer <= 0)
            {
                if(InvulPower)
                {
                    SetInvulPower(false);
                }
            }
            else if(InvulPower && InvulTimer <= InvulPowerDuration * InvulStopWarning && InvulWarningCoroutine == null)
            {
                //Debug.Log("InvulTimer " + InvulTimer);
                InvulWarningCoroutine = StartCoroutine(InvulWarningAnimation());
            }
        }
    }

    public override void TakeDamage(int _Damage, GameObject _Instigator)
    {
        if(InvulPower || InvulTimer > 0)
        {
            return;
        }

        InvulTimer = BlinkDuration;
        base.TakeDamage(_Damage, _Instigator);
    }

    /// <summary>
    /// Handles player death
    /// </summary>
    public override void Death()
    {
        base.Death();

        //Disable potential speed up particle effect and reset velocity.
        Master.MyMovement.Death();

        //Dead sound
        Scr_AudioManager.Instance.PlayOneShot3D(FMODDeath, transform.position);

        //Dead animation
        MyRenderer.GetComponent<Scr_Animation_SpriteAnimator_Player>().StopAllCoroutines();

        //Message GameManager "A Player died", so it can check on current game state
        StartCoroutine(Scr_GameManager.Instance.PlayerDeath(Master));
    }

    /// <summary>
    /// Player is reviving
    /// </summary>
    public void Revive()
    {
      
        ToggleSelectedComponents(true);

        //Alive animation
        MyRenderer.GetComponent<Scr_Animation_SpriteAnimator_Player>().RestartAnimation();

        //Reset points put into Health
        //MaxHealth = BaseMaxHealth;
        //HealthLevel = 1;

        //Reset current health to base health
        ModifyCurrentHealth(MaxHealth);

        //Back to idle animation state
        MyAnimator.SetBool(DeadAnimBool, false);
    }


    /// <summary>
    /// Sets the state for player invulnerability
    /// </summary>
    /// <param name="_State"></param>
    public void SetInvulPower(bool _State)
    {
        InvulPower = _State;

        if(_State)
        {
            InvulTimer = InvulPowerDuration;
            ColoredRenderer.color = InvulTint;
            ColoredRenderer.enabled = true;

            if(NakamaConnection.Instance.IsLocalPlayer(Master.PlayerUsername))
            {
                Scr_UI_OnscreenEffect.Instance.PlayInvulVignette();
            }
        }
        else
        {
            InvulTimer = 0;
            Scr_UI_OnscreenEffect.Instance.StopInvulVignette();
        }
    }

    IEnumerator InvulWarningAnimation()
    {
        //Testing values: 
        //InvulWarningDuration = 3
        //InvulWarningBlinkAmount = 7
        //DurationPerSection = .428
        //isEven = false
        //MultiplicationPerStep = .166

        //Finding the base duration that each step should have
        float DurationPerSection = InvulWarningDuration / InvulWarningBlinkAmount;

        //If BlinkAmount is even, 2 steps will have equal duration
        bool isEven = InvulWarningBlinkAmount % 2 == 0 ? true : false;

        for(int i = InvulWarningBlinkAmount - 1; i > 0; i--)
        {
            float ThisSectionDuration = DurationPerSection;

            if(isEven)
            {
                int LowerHalf = Mathf.FloorToInt((float)InvulWarningBlinkAmount / 2) - 1;
                int Half = Mathf.FloorToInt((float)InvulWarningBlinkAmount / 2);
                float MultiplicationPerStep = .9f / (Half - 1);
                int StepsFromCenter = 0;

                if (i < LowerHalf)
                {
                    StepsFromCenter = i - LowerHalf;
                }
                else if(i > Half)
                {
                    StepsFromCenter = i - Half;
                }

                float Multiplier = 1 + StepsFromCenter * MultiplicationPerStep;
                ThisSectionDuration *= Multiplier;
            }
            else
            {
                int Half = Mathf.FloorToInt((float)InvulWarningBlinkAmount / 2);
                float MultiplicationPerStep = .9f / Half;
                int StepsFromCenter = i - Half;
                float Multiplier = 1 + StepsFromCenter * MultiplicationPerStep;
                ThisSectionDuration *= Multiplier;
            }

            ColoredRenderer.enabled = false;
            yield return new WaitForSeconds(ThisSectionDuration / 2f);
            ColoredRenderer.enabled = true;
            yield return new WaitForSeconds(ThisSectionDuration / 2f);
        }

        ColoredRenderer.enabled = false;
        InvulWarningCoroutine = null;
    }

    public void LevelUpMaxHealth(int _Amount = 1)
    {
        MaxHealth += _Amount;
        HealthLevel += _Amount;
        ModifyCurrentHealth(MaxHealth);
    }

    public override void ModifyCurrentHealth(int _Amount)
    {
        if(_Amount == 0)
        {
            return;
        }

        base.ModifyCurrentHealth(_Amount);
        //SendData(OpCodes.PlayerHealthChange, _Amount.ToString());
       
        HealthChangeDelegate?.Invoke(_Amount);

        // If damaged
        if (_Amount < 0)
        {
            if(!NakamaConnection.Instance.IsLocalPlayer(Master.PlayerUsername))
            {
                return;
            }

            if (CurrentHealth <= DyingThreshold && !IsDying)
            {
                IsDying = true;
                Scr_UI_OnscreenEffect.Instance.PlayDyingVignette();
            }
            else
            {
                Scr_UI_OnscreenEffect.Instance.PlayHurtVignette();
            }
        }
        else
        {
            if (CurrentHealth > DyingThreshold && IsDying)
            {
                IsDying = false;
                Scr_UI_OnscreenEffect.Instance.StopDyingVignette();
            }
        }
    }

    public void Sync(int _ReceivedHealth)
    {
        if(_ReceivedHealth > MaxHealth)
        {
            LevelUpMaxHealth(_ReceivedHealth - MaxHealth);
        }
        else
        {
            if(CurrentHealth <= 0 && _ReceivedHealth > 0)
            {
                Revive();
            }
            else
            {
                ModifyCurrentHealth(_ReceivedHealth - CurrentHealth);
            }
        }
    }

    public override void SendData(long _Code = 0, byte[] _Data = null)
    {
        //if (!NakamaConnection.Instance.IsMultiplayer() || !NakamaConnection.Instance.IsLocalPlayer(Master.PlayerUsername))
        //{
        //    return;
        //}

        //switch (_Code)
        //{
        //    case OpCodes.PlayerHealthChange:
        //        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
        //        break;
        //}
    }

    public override void ReceiveData(IMatchState _MatchState)
    {
        //if (NakamaConnection.Instance.IsLocalPlayer(Master.PlayerUsername))
        //{
        //    return;
        //}

        //switch (_MatchState.OpCode)
        //{
        //    case OpCodes.PlayerHealthChange:
        //        //Network Damage
        //        int HealthChange = int.Parse(MatchDataJSON.DecryptStateToString(_MatchState.State));
        //        Scr_MainThreadDispatcher.Instance.Enqueue(() => ModifyCurrentHealth(HealthChange));
        //        break;
        //}
    }
}
