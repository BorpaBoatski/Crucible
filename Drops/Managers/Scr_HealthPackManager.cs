using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

public class Scr_HealthPackManager : MonoBehaviour, INetworkComponent
{
    [Header("References")]
    public Scr_PowerUp[] HealthPacks;
    public Scr_UI_HealthPackManager UI;

    private void Start()
    {
        AttachToNakama();
    }

    public void DecideHealthPackSpawn()
    {
        int RandomIndex = Random.Range(0, HealthPacks.Length);
        
        for(int i = 0; i < HealthPacks.Length; i++)
        {
            if(i == RandomIndex)
            {
                HealthPacks[i].gameObject.SetActive(true);
            }
            else
            {
                if(HealthPacks[i].gameObject.activeSelf)
                {
                    HealthPacks[i].gameObject.SetActive(false);
                }
            }
        }

        UI.DisplayHint(RandomIndex);
        SendData(OpCodes.HealthPackSpawn, MatchDataJSON.EncryptString(RandomIndex.ToString()));
    }

    #region Networking

    void NetworkSpawn(int _Code)
    {
        //Debug.Log(_Code);

        for (int i = 0; i < HealthPacks.Length; i++)
        {
            if (i == _Code)
            {
                HealthPacks[i].gameObject.SetActive(true);
            }
            else
            {
                if (HealthPacks[i].gameObject.activeSelf)
                {
                    HealthPacks[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        //Debug.Log("Send Data");
        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
    }

    public void ReceiveData(IMatchState _MatchState)
    {
        switch(_MatchState.OpCode)
        {
            case OpCodes.HealthPackSpawn:
                Scr_MainThreadDispatcher.Instance.Enqueue(() => NetworkSpawn(int.Parse(MatchDataJSON.DecryptStateToString(_MatchState.State))));
                break;
        }
    }

    public void AttachToNakama()
    {
        if(!NakamaConnection.Instance.IsMultiplayer() || NakamaConnection.Instance.AmIHost())
        {
            return;
        }

        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }


    #endregion
}
