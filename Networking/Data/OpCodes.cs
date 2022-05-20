using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpCodes
{
    //Initializing
    public const long CharacterSpawn = 10;
    public const long RequestCharacter = 11;
    public const long CharacterSprite = 12;
    public const long StartGame = 13;
    public const long GameLoaded = 14;
    public const long RemindCharacterSprite = 15;

    //Character
    public const long PlayerState = 20;
    public const long Shoot = 21;
    //public const long AxeHit = 22;

    //Gameplay
    public const long NextWave = 30;
    public const long EXPDrop = 31;
    public const long PowerUpDrop = 32;
    public const long HealthPackSpawn = 33;

    //Enemy
    //public const long EnemySpawn = 40;
    //public const long EnemyVelocityPosition = 41;
    //public const long EnemyRefresh = 42;
    //public const long EnemyTakeDamage = 43;
    //public const long EnemyLateSpawn = 44;
    public const long EnemyState = 45;

    //Networking
    public const long Ping = 50;
    public const long Pong = 51;
}
