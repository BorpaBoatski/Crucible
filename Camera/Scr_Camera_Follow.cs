using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Scr_Camera_Follow : MonoBehaviour
{
    [Header("Developer")]
    public float MinDistanceToBeginMoving = 0.01f;

    [Header("Reference")]
    public Transform FollowingObject;
    public Transform PlayerToFollow;
    public BoxCollider2D CameraBounds;
    public TextMeshProUGUI SpectatingMessage;
    private Camera Camera;

    [Header("Properties")]
    Vector2 Movement;
    public bool IsIgnoreBound;
    public bool DebugOutOfBounds;
    public bool IsReadjustingIntoBounds;
    public bool IsInstant;

    private void Awake()
    {
        Camera = GetComponent<Camera>();
    }

    private void FixedUpdate()
    {
        MoveCameraToObject();
        MoveCamera();
    }

    void MoveCameraToObject()
    {
        if(FollowingObject == null)
        {
            ReassignCamera();
            return;
        }

        float DistanceFromCameraToPlayer = Vector2.Distance(transform.position, FollowingObject.position);

        if(transform.position.xy() == FollowingObject.position.xy())
        {
            Movement = Vector3.zero;
            return;
        }

        Movement = FollowingObject.position - transform.position;
    }

    /// <summary>
    /// Camera has lost a target to follow. Go through the list of players to find an elligible player to follow
    /// </summary>
    void ReassignCamera()
    {
        Transform ElligiblePlayer = null;

        for(int i = 0; i < Scr_GameManager.Instance.Players.Count; i++)
        {
            if(Scr_GameManager.Instance.Players[i].GetComponent<Scr_Health>().CurrentHealth > 0)
            {
                ElligiblePlayer = Scr_GameManager.Instance.Players[i].transform;
                break;
            }
            else
            {
                ElligiblePlayer = Scr_GameManager.Instance.Players[i].transform;
            }
        }

        if (ElligiblePlayer != null)
        {
            FollowingObject = ElligiblePlayer;
        }
        else
        {
            //Debug.LogError("No character to follow!");
        }
    }

    void BindToBounds()
    {
        // If current position is out of bounds.
        if(!CameraBounds.OverlapPoint(transform.position))
        {
            DebugOutOfBounds = true;
            return;
        }

        DebugOutOfBounds = false;

        if (IsIgnoreBound)
        {
            return;
        }

        //Test and bind X Movement
        Vector3 NewPosition = transform.position;
        NewPosition.x += Movement.x;
        
        if (!CameraBounds.OverlapPoint(NewPosition))
        {
            Movement.x = 0;
            NewPosition.x = transform.position.x;
        }

        //Test and bind Y Movement
        NewPosition.y += Movement.y;

        if (!CameraBounds.OverlapPoint(NewPosition))
        {
            Movement.y = 0;
        }
    }

    void MoveCamera()
    {
        if(IsReadjustingIntoBounds)
        {
            return;
        }

        if (Movement == Vector2.zero)
        {
            return;
        }

        // If its not instant, move according to delta time.
        if(!IsInstant)
        {
            Movement *= Time.deltaTime * (1 + Movement.magnitude);
        }

        BindToBounds();     
        transform.position += new Vector3(Movement.x, Movement.y, 0);
    }
   
    public void AssignPlayerToFollow(Transform _Player)
    {
        PlayerToFollow = _Player;
        AssignObjectToFollow(PlayerToFollow);

        if(NakamaConnection.Instance.IsMultiplayer() && !NakamaConnection.Instance.IsLocalPlayer(_Player.GetComponent<Scr_Player>().PlayerUsername)) //Camera following network receiver
        {
            SpectatingMessage.enabled = true;
        }
        else //Camera back to local player
        {
            SpectatingMessage.enabled = false;
        }
    }

    public void AssignObjectToFollow(Transform _Object)
    {
        FollowingObject = _Object;
    }


    public IEnumerator ZoomIn(float _Duration, Transform _ZoomInTarget)
    {
        Vector3 _TargetViewPos = Camera.WorldToViewportPoint(_ZoomInTarget.position);

        //if target not within screen
        if (!
           (_TargetViewPos.x > 0 && _TargetViewPos.x < 1 
            && _TargetViewPos.y > 0 && _TargetViewPos.y < 1))
        {

            yield break;
        }

        //Ignore camera bound
        IsIgnoreBound = true;
        //Zoom in towards enemy
        AssignObjectToFollow(_ZoomInTarget);
        //Slow motion.
        Time.timeScale = 0.5f;

        float CummulativeTime = 0.0f;
        float TargetSize = Camera.orthographicSize / 2;

        while (CummulativeTime < _Duration)
        {
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            CummulativeTime += Time.unscaledDeltaTime;

            //Lerp
            float t = CummulativeTime / _Duration;
            t = t * t * (3f - 2f * t);
            float newSize = Mathf.MoveTowards(Camera.orthographicSize, TargetSize, t);
            Camera.orthographicSize = newSize;
        }

        StartCoroutine(ZoomOut(_Duration));
    }

    public IEnumerator ZoomOut(float _Duration)
    {
        //Camera refollow player.
        AssignObjectToFollow(PlayerToFollow);
        //Undo slow motion.
        Time.timeScale = 1;

        _Duration /= 2;
        float CummulativeTime = 0.0f;
        float TargetSize = Camera.orthographicSize * 2;

        while (CummulativeTime < _Duration)
        {
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            CummulativeTime += Time.unscaledDeltaTime;

            //Lerp.
            float t = CummulativeTime / _Duration;
            t = t * t * (3f - 2f * t);
            float newSize = Mathf.MoveTowards(Camera.orthographicSize, TargetSize, t);
            Camera.orthographicSize = newSize;
        }

        if (!CheckIfInBound())
        {
            StartCoroutine(ReadjustIntoBound(_Duration));
        }
        else
        {
            //Rerecognize bound for MoveCamera.
            IsIgnoreBound = false;
        }
    }

    public IEnumerator ReadjustIntoBound(float _Duration)
    {
        IsReadjustingIntoBounds = true;

        Vector3 _ClosestPoint = CameraBounds.ClosestPoint(transform.position);
        Vector3 _Direction = transform.position - _ClosestPoint;
        _Direction.Normalize();

        Vector3 FinalPosition = _ClosestPoint;
        FinalPosition -= _Direction * 0.1f;

        _Duration /= 2;
        float CummulativeTime = 0.0f;

        while (CummulativeTime < _Duration)
        {
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            CummulativeTime += Time.unscaledDeltaTime;

            //Lerp.
            float t = CummulativeTime / _Duration;
            t = t * t * (3f - 2f * t);

            Vector3 _ResetPostion = Vector2.MoveTowards(transform.position, FinalPosition, t);
            transform.position = new Vector3(_ResetPostion.x, _ResetPostion.y, transform.position.z);
        }

        //Rerecognize bound for MoveCamera.
        IsIgnoreBound = false;
        IsReadjustingIntoBounds = false;
    }

    bool CheckIfInBound()
    {
        Vector3 NewPosition = transform.position;

        if (!CameraBounds.OverlapPoint(NewPosition))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public IEnumerator ResetCameraBound()
    {
        while(!CheckIfInBound())
        {
            yield return new WaitForSecondsRealtime(1);
        }

        IsIgnoreBound = false;
    }

}
