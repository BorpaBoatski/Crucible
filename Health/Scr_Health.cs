using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Nakama;

public abstract class Scr_Health : MonoBehaviour, INetworkComponent
{
    [Header("SCR_Health Developer")]
    public int MaxHealth = 1;
    public int DyingThreshold = 1;
    public SpriteRenderer MyRenderer;
    public string DeadAnimBool;

    [Header("BlinkAnimation Developer")]
    public float BlinkDuration = 0.4f;
    public int BlinkAmount;
    public Color BlinkTint = Color.red;

    [Header("SCR_Health Reference")]
    public SpriteRenderer ColoredRenderer;
    public Scr_Animation_SpriteAnimator MySpriteAnimator;
    public Animator MyAnimator;
    public MonoBehaviour[] TogglingComponents;
    public ParticleSystem DeathParticle;
    public ParticleSystem HurtParticle;

    [Header("Properties")]
    protected GameObject Killer;
    public int CurrentHealth { get; protected set; } = 1;
    protected bool IsDying;
    protected Color OriginalTint;
    Coroutine BlinkCoroutine;

    [Header("FMOD")]
    public string FMODHurt;
    public string FMODDeath;

    #region Health Change Delegate
    public delegate void OnCurrentHealthChangeDelegate(int _Amount);
    public OnCurrentHealthChangeDelegate HealthChangeDelegate;
    #endregion

    protected virtual void Awake()
    {
        OriginalTint = MyRenderer.color;
        MyRenderer.material.color = Color.red;
    }

    protected virtual void Start()
    {
        AttachToNakama();
    }

    public void Initialize(int _MaxHealth)
    {
        MaxHealth = _MaxHealth;
        CurrentHealth = MaxHealth;
        ModifyCurrentHealth(MaxHealth);
        //MyAnimator.SetBool(DeadAnimBool, false);
    }

    public virtual void TakeDamage(int _Damage, GameObject _Instigator)
    {
        Killer = _Instigator;
        ModifyCurrentHealth(-_Damage);
    }

    public virtual void ModifyCurrentHealth(int _Amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + _Amount, 0, MaxHealth);

        if(_Amount < 0)
        {
			if (CurrentHealth <= 0)
            {
                Death();
            }
            else
            {
                //Took Damage
                Scr_AudioManager.Instance.PlayOneShot3D(FMODHurt, transform.position);

                if (BlinkCoroutine != null)
                {
                    StopCoroutine(BlinkCoroutine);
                }

                BlinkCoroutine = StartCoroutine(BlinkAnimation());
            }
		}
    }
		
    public void SpillBlood(Vector3 _InstigatorProjectileVelocity)
    {

        if (DeathParticle)
        {
            var main = DeathParticle.main;

            //! Death
            if (CurrentHealth <= 0)
            {
                DeathParticle.transform.rotation = Quaternion.identity;
                DeathParticle.Play();
            }
            else
            {
                Vector3 _ProjVecNormalized = _InstigatorProjectileVelocity.normalized;
                HurtParticle.transform.LookAt(DeathParticle.transform.position + _ProjVecNormalized);
                HurtParticle.Play();
            }
        }

    }

    public virtual void Death()
    {
        MyAnimator.SetBool(DeadAnimBool, true);
        ToggleSelectedComponents(false);
    }

    protected void ToggleSelectedComponents(bool _State)
    {
        //Collider cannot be added into Monobehavior list
        MyAnimator.GetComponent<Collider2D>().enabled = _State;

        for(int i = 0; i < TogglingComponents.Length; i++)
        {
            TogglingComponents[i].enabled = _State;
        }
    }

    protected IEnumerator BlinkAnimation()
    {
        ColoredRenderer.color = BlinkTint;

        for(int i = 0; i < BlinkAmount; i++)
        {
            ColoredRenderer.enabled = true;
            yield return new WaitForSeconds(BlinkDuration / (float)BlinkAmount / 2f);
            ColoredRenderer.enabled = false;
            yield return new WaitForSeconds(BlinkDuration / (float)BlinkAmount / 2f);
        }

        BlinkCoroutine = null;
    }

    public abstract void SendData(long _Code = 0, byte[] _Data = null);
    public abstract void ReceiveData(IMatchState _MatchState);
    public void AttachToNakama()
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }
}
