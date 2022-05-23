using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

public interface INetworkComponent
{
    void SendData(long _Code = 0, byte[] _Data = null);
    void ReceiveData(IMatchState _MatchState);
    void AttachToNakama();
}
