using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scr_Player_Magnet : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("EXP"))
        {
            collision.GetComponent<Scr_Drops_EXP>().BeginAttracting(transform);
        }
    }
}
