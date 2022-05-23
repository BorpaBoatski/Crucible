using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System.Threading.Tasks;
using System.Reflection;
using System;

[System.Serializable]
public class SpawnZoneTable
{
    public Enum_EnemyType MonsterType;
    public SpawnZoneOffset[] Offsets = new SpawnZoneOffset[3];
}

[System.Serializable]
public struct SpawnZoneOffset
{
    public Enum_EnemyRarityType RarityType;

    [Range(0, 8)]
    public int ZoneOffsetBelow5;

    [Range(-8, 0)]
    public int ZoneOffsetAbove9;
}

public class Scr_WaveSpawner : MonoBehaviour, INetworkComponent
{
    [Header("Singleton")]
    public static Scr_WaveSpawner Instance;

    [Header("Developer")]
    [Tooltip("TimeBetweenWaves will be added with SlowMoDuration to get the maximum time between waves")]
    public float TimeBetweenWaves = 3;
    public float SlowMoDuration = 2;
    public GameObject[] MonsterPrefabs;
    public int DurationBetweenSpawns = 1;
    public float DurationForBatch2 = 30;
    public SpawnZoneTable[] SpawningTable = new SpawnZoneTable[3];
    public bool NoSpawn;
    static int MaxEnemiesLimit = 50;
    public LayerMask EnemyLayer;

    [Header("References")]
    public Transform LocalGhostPool;
    public Transform LocalSkeletonPool;
    public Transform LocalSpiderPool;
    public Collider2D[] SpawnZones;

    [Header("Properties")]
    int MainSpawnZone = 0;
    public List<Scr_Enemy> SpawnedMonsters { get; private set; } = new List<Scr_Enemy>();

    public delegate void DelegateEnemyAmountChange();
    public DelegateEnemyAmountChange OnEnemyAmountChange;


    [Header("FMOD")]
    public string FMODRoundEnd;
    public string FMODRoundStart;
    public Enum_WaveStage CurrentWaveStage { get; private set; }

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
    /// Setting of NetworkReceiver if this side is not the host
    /// </summary>
    private void Start()
    {
        AttachToNakama();
    }

    void SpawnTableTest()
    {
        int TestSpawnZone = 0;
        int Offset = 0;

        for (int i = 0; i < SpawningTable.Length; i++)
        {
            for (int m = 1; m < 11; m++)
            {
                MainSpawnZone = m > 5 ? m + 3 : m;
                Debug.Log("Testing Main Zone " + MainSpawnZone);

                for (int j = 0; j < SpawningTable[i].Offsets.Length; j++)
                {
                    if (MainSpawnZone <= 5)
                    {
                        Offset = SpawningTable[i].Offsets[j].ZoneOffsetBelow5 - 1;
                        TestSpawnZone = MainSpawnZone + Offset;
                        Debug.Log("Testing Test Zone " + TestSpawnZone);
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        Offset = SpawningTable[i].Offsets[j].ZoneOffsetAbove9 - 1;
                        TestSpawnZone = MainSpawnZone + Offset;
                        Debug.Log("Testing Test Zone " + TestSpawnZone);
                    }

                    if (TestSpawnZone > SpawnZones.Length - 1 || TestSpawnZone < 0)
                    {
                        Debug.LogError("Spawn Zone failed \n" + "Table " + i + "\nMain Zone " + m + "\nRarity " + j + "\nOffset " + Offset);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Spawn monsters
    /// </summary>
    public void SpawnWave()
    {
        if (NoSpawn)
        {
            return;
        }

        DisableAllEnemyCorpses();

        //A second exectution is made here in case the local player did not revive from the first execution

        if(NakamaConnection.Instance.IsMultiplayer())
        {
            Scr_GameManager.Instance.ReviveLocalPlayer();
        }

#if UNITY_EDITOR
        ClearLog();
#endif
        Scr_UI_SkillCanvas.Instance.CloseUI();
        Scr_AudioManager.Instance.PlayOneShot2D(FMODRoundStart);
        DecideMainSpawnZone();
        CurrentWaveStage = Enum_WaveStage.FIGHTING;

        if (NakamaConnection.Instance.AmIHost())
        {
            StartCoroutine(SpawnSpiders());
            StartCoroutine(SpawnSkeletons());
            StartCoroutine(SpawnGhosts());
        }
    }

    /// <summary>
    /// Decide the main spawn zones to use for spawning offsets
    /// </summary>
    void DecideMainSpawnZone()
    {
        //Max range is 1 above because MAX random is exclusive
        MainSpawnZone = UnityEngine.Random.Range(0, 2) == 0 ? UnityEngine.Random.Range(1, 6) : UnityEngine.Random.Range(10, 13);
    }

    /// <summary>
    /// Spawns spiders based on spawning formula
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnSpiders()
    {
        Enum_EnemyRarityType RarityType = Enum_EnemyRarityType.A;
        int CurrentWave = Scr_GameManager.Instance.CurrentWave;
        int SpiderA = (CurrentWave * 2) + 10;
        int GroupAmount = SpiderA;
        int SpawnZone = MainSpawnZone;

        //Deciding spawn zone for Type B and C
        if (MainSpawnZone <= 5)
        {
            SpawnZone = MainSpawnZone + SpawningTable[0].Offsets[0].ZoneOffsetBelow5;
        }
        else if (MainSpawnZone >= 9)
        {
            SpawnZone = MainSpawnZone + SpawningTable[0].Offsets[0].ZoneOffsetAbove9;
        }

        //Calculation for Type B and C
        for (int j = 0; j < 3; j++)
        {
            switch(j)
            {
                case 1:
                    if(CurrentWave < 5)
                    {
                        yield break;
                    }

                    RarityType = Enum_EnemyRarityType.B;
                    GroupAmount = SpiderA / 3;

                    if (MainSpawnZone <= 5)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[0].Offsets[1].ZoneOffsetBelow5;
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[0].Offsets[1].ZoneOffsetAbove9;
                    }

                    break;
                case 2:
                    if(CurrentWave < 12)
                    {
                        yield break;
                    }

                    RarityType = Enum_EnemyRarityType.C;
                    GroupAmount = SpiderA / 5;

                    if (MainSpawnZone <= 5)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[0].Offsets[2].ZoneOffsetBelow5;
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[0].Offsets[2].ZoneOffsetAbove9;
                    }

                    yield return new WaitForSeconds(DurationForBatch2);
                    break;
            }

            //Spawning
            for (int i = 0; i < GroupAmount; i++)
            {
                if(SpawnedMonsters.Count >= MaxEnemiesLimit)
                {
                    yield return new WaitUntil(() => SpawnedMonsters.Count < MaxEnemiesLimit);
                }

                Scr_Enemy SpawnedEnemy = SelectEnemy(LocalSpiderPool);

                //Spawn formula did not accomodate for how arrays start at 0
                Vector2 SpawnPosition = CreateSpawnPoint(SpawnZone - 1);

                SpawnedEnemy.transform.position = SpawnPosition;
                SpawnedEnemy.OnSpawn(RarityType);
                ModifySpawnedMonsters(SpawnedEnemy, 1);
                //Debug.Log("Sent Enemy Spawn " + SpawnedEnemy.transform.name);
                //SendData(OpCodes.EnemySpawn, MatchDataJSON.EncryptEnemySpawn(Enum_EnemyType.SPIDER, RarityType, SpawnPosition, SpawnedEnemy.transform.name));
                yield return new WaitForSeconds(DurationBetweenSpawns);
            }
        }
    }

    /// <summary>
    /// Spawns skeletons based on spawning formula
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnSkeletons()
    {
        Enum_EnemyRarityType RarityType = Enum_EnemyRarityType.A;
        int CurrentWave = Scr_GameManager.Instance.CurrentWave;
        int SkeletonA = CurrentWave * 2;
        int GroupAmount = SkeletonA;
        int SpawnZone = MainSpawnZone;

        if(MainSpawnZone <= 5)
        {
            SpawnZone = MainSpawnZone + SpawningTable[1].Offsets[0].ZoneOffsetBelow5;
        }
        else if(MainSpawnZone >= 9)
        {
            SpawnZone = MainSpawnZone + SpawningTable[1].Offsets[0].ZoneOffsetAbove9;
        }

        for (int j = 0; j < 3; j++)
        {
            switch(j)
            {
                case 1:
                    if(CurrentWave < 3)
                    {
                        yield break;
                    }

                    RarityType = Enum_EnemyRarityType.B;
                    GroupAmount = SkeletonA / 3;

                    if (MainSpawnZone <= 5)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[1].Offsets[1].ZoneOffsetBelow5;
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[1].Offsets[1].ZoneOffsetAbove9;
                    }

                    break;
                case 2:
                    if(CurrentWave < 15)
                    {
                        yield break;
                    }

                    RarityType = Enum_EnemyRarityType.C;
                    GroupAmount = SkeletonA / 5;

                    if (MainSpawnZone <= 5)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[1].Offsets[2].ZoneOffsetBelow5;
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[1].Offsets[2].ZoneOffsetAbove9;
                    }

                    yield return new WaitForSeconds(DurationForBatch2);
                    break;
            }

            for (int i = 0; i < GroupAmount; i++)
            {
                if (SpawnedMonsters.Count >= MaxEnemiesLimit)
                {
                    yield return new WaitUntil(() => SpawnedMonsters.Count < MaxEnemiesLimit);
                }

                Scr_Enemy SpawnedEnemy = SelectEnemy(LocalSkeletonPool);

                //Spawn formula did not accomodate for how arrays start at 0
                Vector2 SpawnPosition = CreateSpawnPoint(SpawnZone - 1);

                SpawnedEnemy.transform.position = SpawnPosition;
                SpawnedEnemy.OnSpawn(RarityType);
                ModifySpawnedMonsters(SpawnedEnemy, 1);
                //Debug.Log("Sent Enemy Spawn " + SpawnedEnemy.transform.name);
                //SendData(OpCodes.EnemySpawn, MatchDataJSON.EncryptEnemySpawn(Enum_EnemyType.SKELETON, RarityType, SpawnPosition, SpawnedEnemy.transform.name));
                yield return new WaitForSeconds(DurationBetweenSpawns);
            }
        }
    }

    /// <summary>
    /// Spawns ghosts based on spawning formula
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnGhosts()
    {
        Enum_EnemyRarityType RarityType = Enum_EnemyRarityType.A;
        int CurrentWave = Scr_GameManager.Instance.CurrentWave;
        int GhostA = CurrentWave / 5;
        int GroupAmount = GhostA;
        int SpawnZone = MainSpawnZone;

        if (MainSpawnZone <= 5)
        {
            SpawnZone = MainSpawnZone + SpawningTable[2].Offsets[0].ZoneOffsetBelow5;
        }
        else if (MainSpawnZone >= 9)
        {
            SpawnZone = MainSpawnZone + SpawningTable[2].Offsets[0].ZoneOffsetAbove9;
        }

        for (int j = 0; j < 3; j++)
        {
            switch(j)
            {
                case 1:
                    if(CurrentWave < 18)
                    {
                        yield break;
                    }

                    RarityType = Enum_EnemyRarityType.B;
                    GroupAmount = GhostA / 4;

                    if (MainSpawnZone <= 5)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[2].Offsets[1].ZoneOffsetBelow5;
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[2].Offsets[1].ZoneOffsetAbove9;
                    }

                    break;
                case 2:
                    if(CurrentWave < 21)
                    {
                        yield break;
                    }

                    RarityType = Enum_EnemyRarityType.C;
                    GroupAmount = GhostA / 6;

                    if (MainSpawnZone <= 5)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[2].Offsets[2].ZoneOffsetBelow5;
                    }
                    else if (MainSpawnZone >= 9)
                    {
                        SpawnZone = MainSpawnZone + SpawningTable[2].Offsets[2].ZoneOffsetAbove9;
                    }

                    yield return new WaitForSeconds(DurationForBatch2);
                    break;
            }

            for (int i = 0; i < GroupAmount; i++)
            {
                if (SpawnedMonsters.Count >= MaxEnemiesLimit)
                {
                    yield return new WaitUntil(() => SpawnedMonsters.Count < MaxEnemiesLimit);
                }

                Scr_Enemy SpawnedEnemy = SelectEnemy(LocalGhostPool);

                //Spawn formula did not accomodate for how arrays start at 0
                Vector2 SpawnPosition = CreateSpawnPoint(SpawnZone - 1);

                SpawnedEnemy.transform.position = SpawnPosition;
                SpawnedEnemy.OnSpawn(RarityType);
                ModifySpawnedMonsters(SpawnedEnemy, 1);
                //Debug.Log("Sent Enemy Spawn " + SpawnedEnemy.transform.name);
                //SendData(OpCodes.EnemySpawn, MatchDataJSON.EncryptEnemySpawn(Enum_EnemyType.GHOST, RarityType, SpawnPosition, SpawnedEnemy.transform.name));
                yield return new WaitForSeconds(DurationBetweenSpawns);
            }
        }
    }


    /// <summary>
    /// For connected players WaveSpawner to sync spawned monsters
    /// </summary>
    /// <param name="_EnemyType"></param>
    /// <param name="_Rarity"></param>
    /// <param name="_SpawnPoint"></param>
    void NetworkReceiverSpawnEnemy(Dictionary<string, string> _EnemyState)
    {
        Scr_Enemy SpawnedEnemy = null;

        switch ((Enum_EnemyType)int.Parse(_EnemyState["Type"]))
        {
            case Enum_EnemyType.GHOST:
                SpawnedEnemy = SelectEnemy(LocalGhostPool, _EnemyState["Name"]);
                break;
            case Enum_EnemyType.SKELETON:
                SpawnedEnemy = SelectEnemy(LocalSkeletonPool, _EnemyState["Name"]);
                break;
            case Enum_EnemyType.SPIDER:
                SpawnedEnemy = SelectEnemy(LocalSpiderPool, _EnemyState["Name"]);
                break;
        }

        //Debug.Log("NetworkReceiver late spawn " + _EnemyName + ". Current Position: " + SpawnedEnemy.transform.position + " Received Position: " + _SpawnPoint);
        Vector2 ReceivedPosition = new Vector2(float.Parse(_EnemyState["Position X"]), float.Parse(_EnemyState["Position Y"]));
        SpawnedEnemy.transform.position = ReceivedPosition;
        //Debug.Log(SpawnedEnemy.transform.position);
        SpawnedEnemy.OnSpawn((Enum_EnemyRarityType)int.Parse(_EnemyState["Rarity"]));
        Debug.Log("Successfully spawned " + SpawnedEnemy.name);
        ModifySpawnedMonsters(SpawnedEnemy, 1);
    }

    /// <summary>
    /// To spawn enemies. Finding for a specific enemy. Used by clients in Multiplayer
    /// </summary>
    Scr_Enemy SelectEnemy(Transform _Pool, string _SpecificName)
    {
        //Get wave difficulty
        Scr_Enemy Enemy = null;

        //Find unused enemy
        for(int i = 0; i < _Pool.childCount; i++)
        {
            if(_Pool.GetChild(i).name == _SpecificName)
            {
                Enemy = _Pool.GetChild(i).GetComponent<Scr_Enemy>();
                break;
            }
        }

        //Create new enemy
        if(Enemy == null)
        {
            Enemy = Instantiate(GetOriginalCopy(_Pool.GetChild(0).GetComponent<Scr_Enemy>().MyType), _Pool);
            Enemy.name = _SpecificName;

            if(!NakamaConnection.Instance.AmIHost())
            {
                Enemy.SetNetworkReceiver(true);
            }
        }

        return Enemy;
    }
    
    /// <summary>
    /// To spawn enemies
    /// </summary>
    Scr_Enemy SelectEnemy(Transform _Pool)
    {
        //Get wave difficulty
        int CurrentWave = Scr_GameManager.Instance.CurrentWave;
        Scr_Enemy Enemy = null;

        //Find unused enemy
        for(int i = 0; i < _Pool.childCount; i++)
        {
            if(_Pool.GetChild(i).gameObject.activeSelf)
            {
                continue;
            }

            Enemy = _Pool.GetChild(i).GetComponent<Scr_Enemy>();
            break;
        }

        //Create new enemy
        if(Enemy == null)
        {
            Enemy = Instantiate(GetOriginalCopy(_Pool.GetChild(0).GetComponent<Scr_Enemy>().MyType), _Pool);
            Enemy.name = Enemy.MyType.EnemyTypeNormalString() + " (" + (_Pool.transform.childCount - 1).ToString() + ")";

            if (!NakamaConnection.Instance.AmIHost())
            {
                Enemy.SetNetworkReceiver(true);
            }
        }

        return Enemy;
    }

    public int GetEnemyLayerMask()
    {
        return EnemyLayer;
    }

    /// <summary>
    /// Generate a random enemy rarity. The chances for higher rarities increases as more waves pass
    /// </summary>
    /// <returns></returns>
    Enum_EnemyRarityType RandomEnemyRarity()
    {
        int RandomType = UnityEngine.Random.Range(0, 10);
        if(RandomType <= Scr_GameManager.Instance.CurrentWave)
        {
            return Enum_EnemyRarityType.B;
        }
        else if(RandomType <= Scr_GameManager.Instance.CurrentWave / 2)
        {
            return Enum_EnemyRarityType.C;
        }
        else
        {
            return Enum_EnemyRarityType.A;
        }
    }

    /// <summary>
    /// For spawning monsters
    /// </summary>
    /// <returns>Vector2 point withing spawn zones</returns>
    Vector2 CreateSpawnPoint()
    {
        int RandomSpawnZone = UnityEngine.Random.Range(0, SpawnZones.Length);
        return SpawnZones[RandomSpawnZone].PointIn2DCollider();
    }

    /// <summary>
    /// Spawn monster in specific spawn zone
    /// </summary>
    /// <param name="_SpawnZone"></param>
    /// <returns></returns>
    Vector2 CreateSpawnPoint(int _SpawnZone)
    {
        return SpawnZones[_SpawnZone].PointIn2DCollider();
    }

    /// <summary>
    /// After enemies die, this method is executed to check if the wave is complete
    /// </summary>
    public void CheckCurrentWave(Transform _EnemyDeathLocation)
    {
        bool WaveComplete = true;

        for(int i = 0; i < SpawnedMonsters.Count; i++)
        {
            if(SpawnedMonsters[i].MyHealth.CurrentHealth > 0)
            {
                WaveComplete = false;
            }
        }

        if(WaveComplete)
        {
            if(_EnemyDeathLocation != null)
            {
                StartCoroutine(Camera.main.GetComponent<Scr_Camera_Follow>().ZoomIn(SlowMoDuration, _EnemyDeathLocation));
            }

            Scr_AudioManager.Instance.PlayOneShot2D(FMODRoundEnd);
            CurrentWaveStage = Enum_WaveStage.BREAK;
            Scr_GameManager.Instance.NextWave();
            Scr_UI_SkillCanvas.Instance.OpenUI();
        }
    }

    /// <summary>
    /// Disable all spawned monsters object
    /// </summary>
    public void DisableAllEnemyCorpses()
    {
        for(int i = 0; i < SpawnedMonsters.Count; i++)
        {
            SpawnedMonsters[i].gameObject.SetActive(false);
        }

        ModifySpawnedMonsters(null, -2);
    }

    /// <summary>
    /// Message all spawned monsters to update their target. This is executed when it's confirmed that there's another player living
    /// </summary>
    public void UpdateTarget()
    {
        for(int i = 0; i < SpawnedMonsters.Count; i++)
        {
            SpawnedMonsters[i].FindNewTarget();
        }
    }

    public void SendData(long _Code, byte[] _Data = null)
    {
        //if(!NakamaConnection.Instance.IsMultiplayer())
        //{
        //    return;
        //}

        //switch (_Code)
        //{
        //    case OpCodes.EnemySpawn:
        //    case OpCodes.EnemyLateSpawn:
        //    case OpCodes.EnemyRefresh:
        //        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
        //        break;
        //}
    }

    public void ReceiveData(IMatchState _State)
    {
        //if(NakamaConnection.Instance.AmIHost()) //Host side
        //{
        //    switch (_State.OpCode)
        //    {
        //        case OpCodes.EnemyLateSpawn:
        //            //Debug.Log("Sending Late Spawn");
        //            string EnemyName = MatchDataJSON.DecryptStateToString(_State.State);
        //            Scr_MainThreadDispatcher.Instance.Enqueue(() => ResendEnemySpawn(EnemyName));
        //            break;
        //    }
        //}
        //else //Client side
        //{
        //    switch (_State.OpCode)
        //    {
        //        case OpCodes.EnemySpawn:
        //            Dictionary<string, string> EnemyData = MatchDataJSON.DecryptStateToDictionary(_State.State);
        //            //Debug.Log("Received Enemy Spawn " + EnemyData["EnemyName"]);
        //            Scr_MainThreadDispatcher.Instance.Enqueue(() => NetworkReceiverSpawnEnemy(
        //                (Enum_EnemyType)int.Parse(EnemyData["Type"]),
        //                (Enum_EnemyRarityType)int.Parse(EnemyData["Rarity"]),
        //                new Vector2(float.Parse(EnemyData["Spawn X"]), float.Parse(EnemyData["Spawn Y"])),
        //                EnemyData["EnemyName"]
        //                ));
        //            break;

        //        case OpCodes.EnemyVelocityPosition:
        //            Dictionary<string, string> EnemyCheckData = MatchDataJSON.DecryptStateToDictionary(_State.State);
        //            Scr_MainThreadDispatcher.Instance.Enqueue(() =>
        //            {
        //                if (!SpawnedMonsters.Find((x) => x.transform.name == EnemyCheckData["Name"]))
        //                {
        //                    SendData(OpCodes.EnemyLateSpawn, EnemyCheckData["Name"]);
        //                }
        //            });
        //            break;
        //        case OpCodes.EnemyRefresh:
        //            Debug.Log("Late enemy spawn");
        //            Dictionary<string, string> EnemyDataRefresh = MatchDataJSON.DecryptStateToDictionary(_State.State);
        //            //Debug.LogWarning("Received EnemyRefresh for " + EnemyDataRefresh["EnemyName"]);
        //            Scr_MainThreadDispatcher.Instance.Enqueue(() => NetworkReceiverSpawnEnemy(
        //                (Enum_EnemyType)int.Parse(EnemyDataRefresh["Type"]),
        //                (Enum_EnemyRarityType)int.Parse(EnemyDataRefresh["Rarity"]),
        //                new Vector2(float.Parse(EnemyDataRefresh["Spawn X"]), float.Parse(EnemyDataRefresh["Spawn Y"])),
        //                EnemyDataRefresh["EnemyName"],
        //                int.Parse(EnemyDataRefresh["Health"])
        //                ));
        //            break;
        //    }
        //}
        switch (_State.OpCode)
        {
            case OpCodes.EnemyState:
                Dictionary<string, string> EnemyState = MatchDataJSON.DecryptStateToDictionary(_State.State);
                Scr_Enemy FoundEnemy = SpawnedMonsters.Find((x) => x.NetworkName.Equals(EnemyState["Name"]));

                if (FoundEnemy == null && int.Parse(EnemyState["Health"]) > 0)
                {
                    Debug.Log("Spawned " + EnemyState["Name"]);
                    Scr_MainThreadDispatcher.Instance.Enqueue(() => NetworkReceiverSpawnEnemy(EnemyState));
                }

                break;
        }
    }

    public void AttachToNakama()
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        NakamaConnection.Instance.AddToReceiver(ReceiveData);

        if (!NakamaConnection.Instance.AmIHost())
        {
            SetAllEnemiesNetworkReceiver(true);
        }
    }

    void SetAllEnemiesNetworkReceiver(bool _State)
    {
        for (int i = 0; i < 3; i++)
        {
            Transform NetworkSetter = LocalGhostPool;

            switch (i)
            {
                case 0:
                    NetworkSetter = LocalGhostPool;
                    break;
                case 1:
                    NetworkSetter = LocalSkeletonPool;
                    break;
                case 2:
                    NetworkSetter = LocalSpiderPool;
                    break;
            }


            for (int j = 0; j < NetworkSetter.childCount; j++)
            {
                NetworkSetter.GetChild(j).GetComponent<Scr_Enemy>().SetNetworkReceiver(_State);
            }
        }
    }

    Scr_Enemy GetOriginalCopy(Enum_EnemyType _Type)
    {
        for(int i = 0; i < MonsterPrefabs.Length; i++)
        {
            Scr_Enemy ThisEnemy = MonsterPrefabs[i].GetComponent<Scr_Enemy>();

            if (ThisEnemy.MyType == _Type)
            {
                return ThisEnemy;
            }
        }

        return null;
    }

    //void ResendEnemySpawn(string _EnemyName)
    //{
    //    Scr_Enemy LocalEnemy = GetEnemy(_EnemyName);
    //    int CurrentHealth = LocalEnemy.MyHealth.CurrentHealth;

    //    if (CurrentHealth == 0)
    //    {
    //        return;
    //    }

    //    Enum_EnemyRarityType EnemyRarity = LocalEnemy.MyRarityType;
    //    Enum_EnemyType EnemyType = LocalEnemy.MyType;
    //    SendData(OpCodes.EnemyRefresh, MatchDataJSON.EncryptEnemyRefresh(EnemyType, EnemyRarity, LocalEnemy.transform.position, LocalEnemy.transform.name, CurrentHealth));
    //}

    public Scr_Enemy GetEnemy(string _EnemyName)
    {
        Transform Pool = null;

        if (_EnemyName.Contains("Ghost"))
        {
            Pool = LocalGhostPool;
        }
        else if (_EnemyName.Contains("Skeleton"))
        {
            Pool = LocalSkeletonPool;
        }
        else if (_EnemyName.Contains("Spider"))
        {
            Pool = LocalSpiderPool;
        }

        foreach (Transform Child in Pool)
        {
            if(Child.name == _EnemyName)
            {
                return Child.GetComponent<Scr_Enemy>();
            }
        }

        return null;
    }

    public void RemoveEnemyFromList(Scr_Enemy _CleanedEnemy)
    {
        ModifySpawnedMonsters(_CleanedEnemy, -1);
    }

#if UNITY_EDITOR
    void ClearLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
#endif

    public void SwitchToSinglePlayer()
    {
        SetAllEnemiesNetworkReceiver(false);
    }

    /// <summary>
    /// Adds, removes, or clears the SpawnedMonsters list. This is to work in conjunction with the OnEnemyChanged
    /// </summary>
    /// <param name="_Enemy"></param>
    /// <param name="_Case"></param>
    public void ModifySpawnedMonsters(Scr_Enemy _Enemy, int _Case)
    {
        switch (_Case)
        {
            case 1:
                SpawnedMonsters.Add(_Enemy);
                break;
            case -1:
                SpawnedMonsters.Remove(_Enemy);
                break;
            case -2:
                SpawnedMonsters.Clear();
                break;
        }

        OnEnemyAmountChange?.Invoke();
    }
}
