using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats_", menuName = "Database/New EnemyStats")]
public class Scr_Database_Enemy_Stats : ScriptableObject
{
    public float Speed = 1;
    public int MaxHealth = 1;
    public int Damage = 1;
    public int Score = 1;
}
