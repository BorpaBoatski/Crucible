using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Scr_DropManager : MonoBehaviour
{
    [Header("DropManager Developer")]
    public LayerMask WallLayers;

    [Header("DropManager Reference")]
    public Tilemap GroundMap;

    /// <summary>
    /// Tests the drop point for collisions against WallLayers and returns a point that can be reached by the player
    /// </summary>
    /// <param name="_SpawnPoint"></param>
    /// <param name="_DropPoint"></param>
    /// <returns></returns>
    protected Vector3 GenerateReachablePosition(Vector3 _SpawnPoint, Vector3 _DropPoint)
    {
        RaycastHit2D Hit2D;

        Hit2D = Physics2D.CircleCast(_DropPoint, 0.1f, Vector2.zero, 0, WallLayers);

        if (Hit2D.collider != null)
        {
            //Debug.Log(Hit2D.collider.name);

            if (GroundMap.localBounds.Contains(_DropPoint))
            {
                //Debug.Log("Dropped within walkalble area");
                return Hit2D.collider.ClosestPoint(_SpawnPoint);
            }
            else
            {
                //Debug.Log("Dropped outside walkable area");
                Vector3 HalfwayPoint = (GroundMap.localBounds.center - _SpawnPoint) / 10;
                return Hit2D.collider.ClosestPoint(_DropPoint + HalfwayPoint);
            }
        }

        //Debug.Log("No blockage");
        return _SpawnPoint;
    }
}
