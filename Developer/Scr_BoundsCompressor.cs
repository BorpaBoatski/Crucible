using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Scr_BoundsCompressor : MonoBehaviour
{
    [ContextMenu("Recalculate Bounds")]
    void RecalculateBounds()
    {
        GetComponent<Tilemap>().CompressBounds();
    }
}
