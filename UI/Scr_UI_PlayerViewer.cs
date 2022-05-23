using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scr_UI_PlayerViewer : Scr_UIBase
{
    [Header("References")]
    public RectTransform PlayerViewerBG;
    public Camera MainCamera;

    [Header("Properties")]
    //RectTransform BGRect;
    [SerializeField]
    SpriteRenderer NetworkPlayerRenderer;
    Transform LocalPlayer;
    float ViewerBgWidthHalf;
    float ViewerBgHeightHalf;

    public void SetLocalPlayer(Transform _LocalPlayer)
    {
        LocalPlayer = _LocalPlayer;
    }

    [SerializeField]
    Transform NetworkPlayer;
    public void SetNetworkPlayer(Scr_Player _NetworkPlayer)
    {
        NetworkPlayer = _NetworkPlayer.transform;
        NetworkPlayerRenderer = _NetworkPlayer.MyRenderer;
        gameObject.SetActive(true);
    }

    protected override void Awake()
    {
        base.Awake();
        ViewerBgWidthHalf = PlayerViewerBG.rect.width / 2;
        ViewerBgHeightHalf = PlayerViewerBG.rect.height / 2;
    }


    private void FixedUpdate()
    {
        OpenUI();
    }

    public override void OpenUI()
    {
        Vector2 ScreenPoint = MainCamera.WorldToScreenPoint(NetworkPlayer.transform.position);

        //If player is within bounds, close UI and do not update.
        if (ScreenPoint.x > 0 && ScreenPoint.x < MyCanvas.pixelRect.width &&
         ScreenPoint.y > 0 && ScreenPoint.y < MyCanvas.pixelRect.height)
        {
            if (MyCanvas.enabled)
            {
                CloseUI();
            }

            return;
        }

        if (!MyCanvas.enabled)
        {
            base.OpenUI();
        }

        ScreenPoint.x = Mathf.Clamp(ScreenPoint.x, ViewerBgWidthHalf, (MyCanvas.pixelRect.width - ViewerBgWidthHalf));
        ScreenPoint.y = Mathf.Clamp(ScreenPoint.y, ViewerBgHeightHalf, (MyCanvas.pixelRect.height - ViewerBgHeightHalf));

        PlayerViewerBG.transform.position = ScreenPoint;
    }
}
