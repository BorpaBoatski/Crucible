using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

public class Scr_Networking_Enemy : MonoBehaviour, INetworkComponent
{
    [Header("Developer")]
    static float SyncRate = 0.1f;

    [Header("References")]
    Scr_Enemy Enemy;
    Scr_Health_Enemy EnemyHealth;

    #region Properties

    float SyncTimer;
    bool IsReceiver;
    string NetworkName;

    #endregion

    private void Awake()
    {
        SyncTimer = SyncRate;
    }

    private void Start()
    {
        if(NakamaConnection.Instance.IsMultiplayer() && !NakamaConnection.Instance.AmIHost())
        {
            AttachToNakama();
        }
    }

    void LateUpdate()
    {
        if(!IsReceiver)
        {
            if (SyncTimer <= 0)
            {
                SyncTimer = SyncRate;
                SendSync();
            }
            else
            {
                SyncTimer -= Time.deltaTime;
            }
        }
    }

    void SendSync()
    {
        //SendData(OpCodes.EnemyState, MatchDataJSON.EncryptEnemyState(NetworkName, ));
    }

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
    }

    public void ReceiveData(IMatchState _MatchState)
    {

    }

    public void AttachToNakama()
    {
        IsReceiver = true;
        NetworkName = transform.name;
        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }
}
