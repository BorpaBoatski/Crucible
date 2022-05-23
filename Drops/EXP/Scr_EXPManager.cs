using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using UnityEngine.Tilemaps;

public class Scr_EXPManager : Scr_DropManager
{
    [Header("Singleton")]
    public static Scr_EXPManager Instance;

    [Header("ToSpawn")]
    public GameObject RedEXP;
    public GameObject BlueEXP;
    public GameObject YellowEXP;

    [Header("References")]
    public Transform BlueEXPPool;
    public Transform YellowEXPPool;
    public Transform RedEXPPool;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Spawns EXP based on EXPType
    /// </summary>
    /// <param name="_Origin"></param>
    /// <param name="_EXPType"></param>
    public void SpawnEXP(Vector2 _Origin, Enum_EXPType _EXPType)
    {
        Scr_Drops_EXP[] EXPPool = new Scr_Drops_EXP[1];

        switch (_EXPType)
        {
            case Enum_EXPType.BLUE:
                EXPPool = GetUnusedEXPs(Enum_EXPType.BLUE, 1);
                break;
            case Enum_EXPType.YELLOW:
                EXPPool = GetUnusedEXPs(Enum_EXPType.YELLOW, 1);
                break;
            case Enum_EXPType.RED:
                EXPPool = GetUnusedEXPs(Enum_EXPType.RED, 1);
                break;
        }

        for (int i = 0; i < EXPPool.Length; i++)
        {
            EXPPool[i].transform.position = GenerateReachablePosition(_Origin, _Origin);
            //SendData(OpCodes.EXPDrop, MatchDataJSON.EncryptEXPDrop(_EXPType, _Origin));
            EXPPool[i].gameObject.SetActive(true);
        }
    }

    Scr_Drops_EXP[] GetUnusedEXPs(Enum_EXPType _EXPType, int _Amount)
    {
        Transform _Pool = BlueEXPPool;

        switch(_EXPType)
        {
            case Enum_EXPType.YELLOW:
                _Pool = YellowEXPPool;
                break;
            case Enum_EXPType.RED:
                _Pool = RedEXPPool;
                break;
        }

        Scr_Drops_EXP[] EXPs = new Scr_Drops_EXP[_Amount];

        for(int i = 0; i < _Amount; i++)
        {
            for(int j = 0; j < _Pool.childCount; j++)
            {
                if (!_Pool.GetChild(j).gameObject.activeSelf)
                {
                    EXPs[i] = _Pool.GetChild(j).gameObject.GetComponent<Scr_Drops_EXP>();
                    break;
                }
            }

            if(EXPs[i] == null)
            {
                switch(_EXPType)
                {
                    case Enum_EXPType.BLUE:
                        EXPs[i] = Instantiate(BlueEXP, _Pool).GetComponent<Scr_Drops_EXP>();
                        EXPs[i].name = BlueEXP.name + " (" + (_Pool.childCount - 1).ToString() + ")";
                        break;
                    case Enum_EXPType.YELLOW:
                        EXPs[i] = Instantiate(YellowEXP, _Pool).GetComponent<Scr_Drops_EXP>();
                        EXPs[i].name = YellowEXP.name + " (" + (_Pool.childCount - 1).ToString() + ")";
                        break;
                    case Enum_EXPType.RED:
                        EXPs[i] = Instantiate(RedEXP, _Pool).GetComponent<Scr_Drops_EXP>();
                        EXPs[i].name = RedEXP.name + " (" + (_Pool.childCount - 1).ToString() + ")";
                        break;
                }
            }
        }

        return EXPs;
    }

    public Scr_Drops_EXP GetEXP(string _Name)
    {
        Transform Pool = null;
        GameObject PrefabToSpawn = null;

        if (_Name.Contains(BlueEXP.name))
        {
            Pool = BlueEXPPool;
            PrefabToSpawn = BlueEXP;
        }
        else if(_Name.Contains(YellowEXP.name))
        {
            Pool = YellowEXPPool;
            PrefabToSpawn = YellowEXP;
        }
        else if(_Name.Contains(RedEXP.name))
        {
            Pool = RedEXPPool;
            PrefabToSpawn = RedEXP;
        }

        foreach (Transform Child in Pool)
        {
            if(Child.name == _Name)
            {
                return Child.GetComponent<Scr_Drops_EXP>();
            }
        }

        Scr_Drops_EXP NewEXP = Instantiate(PrefabToSpawn, Pool).GetComponent<Scr_Drops_EXP>();
        NewEXP.name = _Name;
        return NewEXP;
    }
}
