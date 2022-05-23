using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClientConnection_", menuName = "Database/New ClientConnection")]
public class Scr_Database_ClientConnection : ScriptableObject
{
    public string scheme = "http";
    public string host = "localhost";
    public int port = 7350;
    public string serverKey = "defaultkey";
}
