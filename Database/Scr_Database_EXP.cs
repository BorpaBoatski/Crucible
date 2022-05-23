using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EXPTable
{
    public Enum_EXPType EXPType;
    public int EXPAmount;
}


[CreateAssetMenu(fileName = "EXP_", menuName = "Database/New EXP")]
public class Scr_Database_EXP : ScriptableObject
{
    public EXPTable[] EXPValues;

    public int ReturnEXPAmount(Enum_EXPType _Type)
    {
        foreach(EXPTable EXP in EXPValues)
        {
            if(EXP.EXPType == _Type)
            {
                return EXP.EXPAmount;
            }
        }

        return 0;
    }
}
