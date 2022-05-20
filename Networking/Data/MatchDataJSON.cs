using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama.TinyJson;
using System.Text;
using System;

public static class MatchDataJSON
{
    public static byte[] EncryptVelocityAndPosition(Vector2 _Velocity, Vector2 _Position)
    {
        Dictionary<string, string> PositionData = new Dictionary<string, string>
        {
            {"Velocity X", _Velocity.x.ToString()},
            {"Velocity Y", _Velocity.y.ToString()},
            {"Position X", _Position.x.ToString()},
            {"Position Y", _Position.y.ToString()}
            
        };

        return Encoding.UTF8.GetBytes(PositionData.ToJson());
    }

    //public static string EncryptEnemyVelocityAndPosition(Vector2 _Velocity, Vector2 _Position, string _Name)
    //{
    //    Dictionary<string, string> PositionData = new Dictionary<string, string>
    //    {
    //        {"Velocity X", _Velocity.x.ToString()},
    //        {"Velocity Y", _Velocity.y.ToString()},
    //        {"Position X", _Position.x.ToString()},
    //        {"Position Y", _Position.y.ToString()},
    //        {"Name", _Name}
    //    };

    //    return PositionData.ToJson();
    //}

    //public static string EncryptEXPPickUp(int _SibilingIndex, Enum_EXPType _EXPType)
    //{
    //    Dictionary<string, string> EXPData = new Dictionary<string, string>
    //    {
    //        {"EXP", ((int)_EXPType).ToString()},
    //        {"Index", _SibilingIndex.ToString()}
    //    };

    //    return EXPData.ToJson();
    //}

    public static byte[] EncryptString(string _String)
    {
        return Encoding.UTF8.GetBytes(_String);
    }

    public static string DecryptStateToString(byte[] _State)
    {
        return Encoding.UTF8.GetString(_State);
    }

    public static Dictionary<string, string> DecryptStateToDictionary(byte[] _State)
    {
        return Encoding.UTF8.GetString(_State).FromJson<Dictionary<string, string>>();
    }

    //public static string MovementInput(Vector2 _Input)
    //{
    //    Dictionary<string, string> MovementInputData = new Dictionary<string, string>
    //    {
    //        {"Horizontal", _Input.x.ToString()},
    //        {"Vertical", _Input.y.ToString()}
    //    };

    //    return MovementInputData.ToJson();
    //}

    //public static string EncryptAimDirection(Vector2 _AimDirection)
    //{
    //    Dictionary<string, string> MouseInputData = new Dictionary<string, string>
    //    {
    //        {"AimDirection X", _AimDirection.x.ToString()},
    //        {"AimDirection Y", _AimDirection.y.ToString()}
    //    };

    //    return MouseInputData.ToJson();
    //}

    public static byte[] EncryptShoot(string _AxeName, Vector2 _AimDirection, bool IsPiercing, int _Damage)
    {
        Dictionary<string, string> MouseInputData = new Dictionary<string, string>
        {
            {"AimDirection X", _AimDirection.x.ToString()},
            {"AimDirection Y", _AimDirection.y.ToString()},
            {"AxeName", _AxeName},
            {"IsPiercing", IsPiercing.ToString()},
            {"Damage", _Damage.ToString()},
        };

        return Encoding.UTF8.GetBytes(MouseInputData.ToJson());
    }

    //public static string EncryptCharacterData(Vector2 _SpawnPosition, string _URL)
    //{
    //    Dictionary<string, string> MouseInputData = new Dictionary<string, string>
    //    {
    //        {"Position X", _SpawnPosition.x.ToString()},
    //        {"Position Y", _SpawnPosition.y.ToString()},
    //        {"URL", _URL}
    //    };

    //    return MouseInputData.ToJson();
    //}

    //public static string EncryptEnemySpawn(Enum_EnemyType _EnemyType, Enum_EnemyRarityType _EnemyRarity, Vector2 _Position, string _EnemyName)
    //{
    //    Dictionary<string, string> EnemyData = new Dictionary<string, string>
    //    {
    //        {"Type", ((int)_EnemyType).ToString()},
    //        {"Rarity", ((int)_EnemyRarity).ToString()},
    //        {"Spawn X", _Position.x.ToString()},
    //        {"Spawn Y", _Position.y.ToString()},
    //        {"EnemyName", _EnemyName}
    //    };

    //    return EnemyData.ToJson();
    //}

    //public static string EncryptEnemyRefresh(Enum_EnemyType _EnemyType, Enum_EnemyRarityType _EnemyRarity, Vector2 _Position, string _EnemyName, int _CurrentHealth)
    //{
    //    Dictionary<string, string> EnemyData = new Dictionary<string, string>
    //    {
    //        {"Type", ((int)_EnemyType).ToString()},
    //        {"Rarity", ((int)_EnemyRarity).ToString()},
    //        {"Spawn X", _Position.x.ToString()},
    //        {"Spawn Y", _Position.y.ToString()},
    //        {"EnemyName", _EnemyName},
    //        {"Health", _CurrentHealth.ToString()}
    //    };

    //    return EnemyData.ToJson();
    //}

    public static byte[] EncryptPowerUpSpawn(int _PowerUpType, Vector2 _Position)
    {
        Dictionary<string, string> EnemyData = new Dictionary<string, string>
        {
            {"Type", _PowerUpType.ToString()},
            {"Spawn X", _Position.x.ToString()},
            {"Spawn Y", _Position.y.ToString()}
        };

        return Encoding.UTF8.GetBytes(EnemyData.ToJson());
    }

    //public static string EncryptEnemyHealthChange(string _Name, int _Damage)
    //{
    //    Dictionary<string, string> EnemyData = new Dictionary<string, string>
    //    {
    //        {"Name", _Name},
    //        {"Damage", _Damage.ToString()}
    //    };

    //    return EnemyData.ToJson();
    //}

    //public static string EncryptEXPDrop(Enum_EXPType _Type, Vector2 _Position)
    //{
    //    Dictionary<string, string> EnemyData = new Dictionary<string, string>
    //    {
    //        {"Type", ((int)_Type).ToString()},
    //        {"Position X", _Position.x.ToString()},
    //        {"Position Y", _Position.y.ToString()}
    //    };

    //    return EnemyData.ToJson();
    //}

    //public static string EncryptAxeHit(string _AxeName, int _Damage, string _OwnerID, string _EnemyName)
    //{
    //    Dictionary<string, string> AxeHit = new Dictionary<string, string>
    //    {
    //        {"AxeName", _AxeName},
    //        {"Damage", _Damage.ToString()},
    //        {"OwnerID", _OwnerID},
    //        {"EnemyName", _EnemyName},
    //    };

    //    return AxeHit.ToJson();
    //}

    //public static string EncryptWaveStart(DateTime _CurrentTime, int _CurrentWave)
    //{
    //    Dictionary<string, string> AxeHit = new Dictionary<string, string>
    //    {
    //        {"Time", _CurrentTime.ToString()},
    //        {"Wave", _CurrentWave.ToString()},
    //    };

    //    return AxeHit.ToJson();
    //}

    //public static string EncryptColor(Color _Color)
    //{
    //    Dictionary<string, string> AxeHit = new Dictionary<string, string>
    //    {
    //        {"R", _Color.r.ToString()},
    //        {"G", _Color.g.ToString()},
    //        {"B", _Color.b.ToString()},
    //    };

    //    return AxeHit.ToJson();
    //}

    public static byte[] EncryptPlayerState(Vector2 _Velocity, Vector2 _Position, Vector2 _AimDirection, int _Health, int _Level, int _EXP, int _Score)
    {
        Dictionary<string, string> PlayerState = new Dictionary<string, string>
        {
            {"Velocity X", _Velocity.x.ToString()},
            {"Velocity Y", _Velocity.y.ToString()},
            {"Position X", _Position.x.ToString()},
            {"Position Y", _Position.y.ToString()},
            {"AimDirection X", _AimDirection.x.ToString()},
            {"AimDirection Y", _AimDirection.y.ToString()},
            {"Health", _Health.ToString()},
            {"Level", _Level.ToString()},
            {"EXP", _EXP.ToString()},
            {"Score", _Score.ToString()},
        };

        return Encoding.UTF8.GetBytes(PlayerState.ToJson());
    }

    public static byte[] EncryptEnemyState(string _Name, Vector2 _Velocity, Vector2 _Position, int _Health, Enum_EnemyRarityType _Rarity, Enum_EnemyType _Type)
    {
        Dictionary<string, string> EnemyState = new Dictionary<string, string>
        {
            {"Name", _Name},
            {"Velocity X", _Velocity.x.ToString()},
            {"Velocity Y", _Velocity.y.ToString()},
            {"Position X", _Position.x.ToString()},
            {"Position Y", _Position.y.ToString()},
            {"Health", _Health.ToString()},
            {"Rarity", ((int)_Rarity).ToString()},
            {"Type", ((int)_Type).ToString()},
        };

        return Encoding.UTF8.GetBytes(EnemyState.ToJson());
    }
}
