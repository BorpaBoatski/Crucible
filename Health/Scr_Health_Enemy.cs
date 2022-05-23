using Nakama;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scr_Health_Enemy : Scr_Health
{
    [Header("Developer")]
    static float DurationTillDisable = 2;

    public Scr_Enemy Master { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Master = GetComponent<Scr_Enemy>();
    }

    private void OnEnable()
    {
        ToggleSelectedComponents(true);
    }

    public override void TakeDamage(int _Damage, GameObject _Instigator)
    {
        //Player projectile already does local player check
        //if (_Instigator != null)
        //{
        //    SendData(OpCodes.EnemyTakeDamage, MatchDataJSON.EncryptEnemyHealthChange(transform.name, _Damage));
        //    //Master.Retaliate(_Instigator);
        //}

        if (_Instigator != Master.Target)
        {
            Master.Retaliate(_Instigator);
        }

        base.TakeDamage(_Damage, _Instigator);
    }

    public override void Death()
    {
        base.Death();
        Master.IdleSoundScript.StopIdleSound();
        Master.EnemyDeath();
        Scr_AudioManager.Instance.PlayOneShot3D(FMODDeath, transform.position);
        AddToPlayerScore();
        MySpriteAnimator.StopAllCoroutines();
        Scr_PowerUps_Manager.Instance.SpawnPowerUp(transform.position);
        Scr_WaveSpawner.Instance.CheckCurrentWave(transform);
        Scr_WaveSpawner.Instance.RemoveEnemyFromList(Master);

        if(gameObject.activeSelf)
        {
            StartCoroutine(CleanUpBody());
        }
    }

    IEnumerator CleanUpBody()
    {
        if(!gameObject.activeSelf) //Body already cleaned
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(DurationTillDisable);
        gameObject.SetActive(false);
    }

    void AddToPlayerScore()
    {
        if(Killer == null)
        {
            return;
        }

        if(NakamaConnection.Instance.IsLocalPlayer(Killer.GetComponent<Scr_Player>().PlayerUsername))
        {
            Killer.GetComponent<Scr_Player>().EarnScore(Master.StatPresets[(int)Master.MyRarityType].Score);
        }
    }

    public void SetKiller(GameObject _Killer)
    {
        Killer = _Killer;
    }

    #region Networking

    public void Sync(int _ReceivedHealth)
    {
        if(_ReceivedHealth == CurrentHealth)
        {
            return;
        }

        ModifyCurrentHealth(_ReceivedHealth - CurrentHealth);
    }

    public override void ReceiveData(IMatchState _MatchState)
    {
        //switch (_MatchState.OpCode)
        //{
        //    case OpCodes.EnemyTakeDamage:
        //        Dictionary<string, string> EnemyTakeDamageData = MatchDataJSON.DecryptStateToDictionary(_MatchState.State);

        //        Scr_MainThreadDispatcher.Instance.Enqueue(() =>
        //        {
        //            if (transform.name != EnemyTakeDamageData["Name"])
        //            {
        //                return;
        //            }

        //            int Damage = int.Parse(EnemyTakeDamageData["Damage"]);
        //            TakeDamage(Damage, null);
        //        });
        //        break;
        //}
    }

    public override void SendData(long _Code = 0, byte[] _Data = null)
    {
        //if (!NakamaConnection.Instance.IsMultiplayer())
        //{
        //    return;
        //}

        //switch (_Code)
        //{
        //    case OpCodes.EnemyTakeDamage:
        //        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
        //        break;
        //}
    }

    #endregion
}
