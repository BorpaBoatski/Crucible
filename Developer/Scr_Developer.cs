using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Scr_Developer
{
    public static Vector2 xy(this Vector3 _Vec3)
    {
        return new Vector2(_Vec3.x, _Vec3.y);
    }

    public static Vector2 PointIn2DCollider(this Collider2D _Collider)
    {
        Vector2 Point = _Collider.bounds.center;
        Point.x += Random.Range(0, _Collider.bounds.size.x) - (_Collider.bounds.size.x / 2);
        Point.y += Random.Range(0, _Collider.bounds.size.y) - (_Collider.bounds.size.y / 2);
        return Point;
    }

    public static string EnemyTypeNormalString(this Enum_EnemyType _Type)
    {
        switch(_Type)
        {
            case Enum_EnemyType.GHOST:
                return "Ghost";
            case Enum_EnemyType.SKELETON:
                return "Skeleton";
            case Enum_EnemyType.SPIDER:
                return "Spider";
            default:
                return "";
        }
    }
}
