using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nakama;
using System;

public class Scr_UI_PingCanvas : Scr_UIBase, INetworkComponent
{
    [Header("Developer")]
    public Color[] PingColors;
    public float SyncRate = 0.1f;

    [Header("References")]
    public Image[] PingLevels;

    [Header("Properties")]
    TimeSpan LastElapsedTime;
    float SyncTimer = 0;
    DateTime PingTime;

    private void Start()
    {
        AttachToNakama();
    }

    private void FixedUpdate()
    {
        if (SyncTimer <= 0)
        {
            SendData(OpCodes.Ping, MatchDataJSON.EncryptString(DateTime.Now.ToString()));
            SyncTimer = SyncRate;
        }
        else
        {
            SyncTimer -= Time.fixedDeltaTime;
        }
    }

    private void LateUpdate()
    {
        UpdatePing(LastElapsedTime.Milliseconds);
    }

    public void UpdatePing(int _Milliseconds)
    {
        UpdateColor(_Milliseconds < 50 ? 2 : _Milliseconds < 100 ? 1 : 0);
    }

    void UpdateColor(int _Level)
    {
        for(int i = 0; i < PingLevels.Length; i++)
        {
            if(i <= _Level)
            {
                PingLevels[i].color = PingColors[_Level];
            }
            else
            {
                PingLevels[i].color = Color.white;
            }
        }
    }

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        switch (_Code)
        {
            case OpCodes.Ping:
                //Debug.Log("Sent Ping");
                PingTime = DateTime.Now;
                NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
                break;
            case OpCodes.Pong:
                //Debug.Log("Sent Pong");
                NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
                break;
        }
    }

    public void ReceiveData(IMatchState _MatchState)
    {
        switch (_MatchState.OpCode)
        {
            case OpCodes.Ping:
                //Debug.Log("Received Ping");
                //Debug.Log("Time Now: " + DateTime.Now.Ticks + "\nTime Sent: " + Time.Ticks + "\nPing: " + LastElapsedTime.Milliseconds);
                SendData(OpCodes.Pong);
                break;
            case OpCodes.Pong:
                //Debug.Log("Received Pong");
                LastElapsedTime = DateTime.Now - PingTime;
                //Debug.Log(LastElapsedTime);
                break;
        }
    }

    public void AttachToNakama()
    {
        if (NakamaConnection.Instance.IsMultiplayer())
        {
            NakamaConnection.Instance.AddToReceiver(ReceiveData);
        }
    }
}
