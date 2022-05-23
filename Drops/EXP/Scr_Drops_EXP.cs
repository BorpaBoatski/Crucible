using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Nakama;

public class Scr_Drops_EXP : MonoBehaviour
{
    [Header("Developer")]
    public float DurationTillDisappear = 15;
    public Enum_EXPType EXPType;
    public Scr_Database_EXP EXPDatabase;

    [Header("Reference")]
    public SpriteRenderer MyRenderer;
    public Collider2D MyCollider;

    [Header("Properties")]
    Sequence ColorShineAnimation;
    Coroutine FadeOutRoutine;
    Transform Attractor;
    float Speed = 0.1f;

    [Header("FMOD")]
    public string FMODPickUp;

    private void OnEnable()
    {
        MyRenderer.enabled = true;
        Attractor = null;
        FadeOutRoutine = StartCoroutine(FadeOut());
        transform.rotation = Quaternion.identity;
    }

    private void FixedUpdate()
    {
        if(Attractor != null)
        {
            transform.up = Attractor.transform.position - transform.position;
            transform.position += Vector3.ClampMagnitude(Attractor.transform.position - transform.position, Speed);
            Speed += Time.fixedDeltaTime;
        }
    }

    public void BeginAttracting(Transform _Attractor)
    {
        if(Attractor != null)
        {
            return;
        }

        Attractor = _Attractor;
        Speed = 0.1f;
        StopCoroutine(FadeOutRoutine);
    }

    /// <summary>
    /// Time to fade out EXP
    /// </summary>
    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(DurationTillDisappear - 1);

        int Blinks = 3;
        float BlinkPause = 1f / Blinks / 2;

        for(int i = 0; i < Blinks; i++)
        {
            MyRenderer.enabled = false;
            yield return new WaitForSeconds(BlinkPause);
            MyRenderer.enabled = true;
            yield return new WaitForSeconds(BlinkPause);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Return how much this EXP drop is worth
    /// </summary>
    /// <returns></returns>
    public int GetEXP()
    {
        return EXPDatabase.ReturnEXPAmount(EXPType);
    }

    //private void OnTriggerStay2D(Collider2D collision)
    //{
    //    if(collision.CompareTag("Player"))
    //    {
    //        return;
    //    }

    //    ColliderDistance2D dist = collision.Distance(MyCollider);
    //    Debug.DrawLine(dist.pointA, dist.pointB, Color.red);
    //    Vector3 _ChangeResult = dist.pointA - dist.pointB;

    //    /*Vector3 MidMapLocation = new Vector3(30, 35, 0);
    //    Vector3 _ChangeResult = transform.position - MidMapLocation;
    //    _ChangeResult.Normalize();
    //    transform.position = transform.position - _ChangeResult;*/

    //    transform.position = transform.position + _ChangeResult;
    //}


    /// <summary>
    /// Executed when player walks over EXP
    /// </summary>
    public void PickUp()
    {
        ColorShineAnimation.Pause();

        if(FadeOutRoutine != null)
        {
            StopCoroutine(FadeOutRoutine);
        }

        gameObject.SetActive(false);
        Scr_AudioManager.Instance.PlayOneShot3D(FMODPickUp,transform.position);
    }
}
