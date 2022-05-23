using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scr_Player_Animator : MonoBehaviour
{
    [Header("Developer")]
    public string MovementAnimFloat;

    [Header("Properties")]
    Animator MyAnimator;
    Scr_Player ThePlayer;

    public void LinkPlayer(Scr_Player _Player)
    {
        ThePlayer = _Player;
    }

    private void Awake()
    {
        MyAnimator = GetComponent<Animator>();
    }

    public void InvulPowerAnimationStart()
    {
        MyAnimator.SetBool("InvulPower", true);
    }

    public void InvulHitAnimationStart()
    {
        MyAnimator.SetTrigger("InvulHit");
    }

    public void AnimateMovement(Vector2 _Direction)
    {
        MyAnimator.SetFloat(MovementAnimFloat, _Direction.x);
    }
}
