using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class Scr_UI_HealthPackManager : MonoBehaviour
{
    [Header("References")]
    TextMeshProUGUI TextLocationHint;

    private void Awake()
    {
        TextLocationHint = GetComponent<TextMeshProUGUI>();
    }

    public void DisplayHint(int _RandomIndex)
    {
        TextLocationHint.color = Color.white;

        switch (_RandomIndex)
        {
            case 0:
                TextLocationHint.text = "Healing available in the Hedge Maze";
                break;
            case 1:
                TextLocationHint.text = "Healing available at the Ponds";
                break;
            case 2:
                TextLocationHint.text = "Healing available at the Graveyard";
                break;
            case 3:
                TextLocationHint.text = "Healing available at the Crossroads";
                break;
        }

        TextLocationHint.DOColor(Color.clear, 10).SetEase(Ease.InExpo);
    }
}
