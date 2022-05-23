using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scr_EnemyIdleSound : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] string FMODIdleSound;
    [SerializeField] float IdleSoundCd;
    [SerializeField] Enum_EnemyType MyType;

    [Header("References")]
    [SerializeField] Scr_Health_Enemy MyHealth;
    FMOD.Studio.EventInstance SoundEvent;
    private void Awake()
    {
        MyHealth = GetComponent<Scr_Health_Enemy>();
    }

    private void OnEnable()
    {
        StartCoroutine(IdleSoundLoop());
    }
    public void TryPlayIdleSound()
    {
        if (MyHealth.CurrentHealth <= 0)
        {
            return;
        }

        if(EnemyIdleSoundManager.Instance.CheckIntervalPlayability(MyType))
        {
            SoundEvent = FMODUnity.RuntimeManager.CreateInstance(FMODIdleSound);
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(SoundEvent, transform);
            SoundEvent.start();
            SoundEvent.release();
        }
    }

    public void StopIdleSound()
    {
        if(SoundEvent.isValid())
        {
            SoundEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    IEnumerator IdleSoundLoop()
    {
        yield return new WaitForSecondsRealtime(IdleSoundCd);
        TryPlayIdleSound();
        StartCoroutine(IdleSoundLoop());
    }

}
