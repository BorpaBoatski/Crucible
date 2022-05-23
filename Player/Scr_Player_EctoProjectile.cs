using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

public class Scr_Player_EctoProjectile : MonoBehaviour, INetworkComponent
{
    [Header("Developer")]
    public float SpeedMultiplier = 2;
    public bool IsReceiver;
    public float RotationSpeed = 2;

    [Header("References")]
    public Scr_Player Owner;
    public GameObject Trial;
    public GameObject PierceTrial;

    [Header("Properties")]
    Rigidbody2D MyRigidbody;
    int Damage;
    bool IsPierce;
    SpriteRenderer MyRenderer;
    float Xscale;
    bool Flipped;
    Vector2 FiredVelocity;
    bool CanDisappear;

    [Header("FMOD")]
    public string FMODAxeImpact;
    public string FMODAxeThrow;

    void Initialize()
    {
        MyRigidbody = GetComponent<Rigidbody2D>();
        MyRenderer = GetComponent<SpriteRenderer>();
        Xscale = Mathf.Abs(transform.localScale.x);
        AttachToNakama();
    }
    private void FixedUpdate()
    {
        if (!MyRenderer.isVisible && CanDisappear)
        {
            gameObject.SetActive(false);
        }

        Rotate();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "Enemy":
                Scr_Health_Enemy HitEnemy = collision.attachedRigidbody.GetComponent<Scr_Health_Enemy>();

                if (NakamaConnection.Instance.AmIHost())
                {
                    HitEnemy.TakeDamage(Damage, Owner.gameObject);
                    //Debug.Log("Send Axe Hit " + HitEnemy.name);
                    //SendData(OpCodes.AxeHit, MatchDataJSON.EncryptAxeHit(transform.name, Damage, Owner.PlayerUsername, HitEnemy.transform.name));
                    HitEnemy.StartCoroutine(HitEnemy.Master.DamagedSlowDown());
                }
                else
                {
                    HitEnemy.SetKiller(Owner.gameObject);
                }

                HitEnemy.SpillBlood(MyRigidbody.velocity);

                if (IsPierce)
                {
                    Scr_AudioManager.Instance.PlayOneShot3D(FMODAxeImpact, transform.position);
                    return;
                }

                break;
        }

        gameObject.SetActive(false);
        Scr_AudioManager.Instance.PlayOneShot3D(FMODAxeImpact, transform.position);
    }

    /// <summary>
    /// Use this to activate projectiles
    /// </summary>
    /// <param name="_Movement"></param>
    /// <param name="_Damage"></param>
    /// <param name="_SpawnPoint"></param>
    public void ShootThisProjectile(Vector2 _Movement, int _Damage, Transform _SpawnPoint, bool FlipX, bool IsPierce)
    {
        if(MyRigidbody == null)
        {
            Initialize();
        }

        this.IsPierce = IsPierce;
        
        if(this.IsPierce)
        {
            PierceTrial.SetActive(true);
            Trial.SetActive(false);
        }
        else
        {
            Trial.SetActive(true);
            PierceTrial.SetActive(false);
        }

        Flipped = FlipX;

        Vector3 _Scale = FlipX ? 
                         new Vector3(-Xscale, transform.localScale.y,transform.localScale.z) 
                         : new Vector3(Xscale, transform.localScale.y, transform.localScale.z);

        transform.position = _SpawnPoint.transform.position;
        transform.localScale = _Scale;

        gameObject.SetActive(true);
        
        StartCoroutine(AllowDisappearance());
        
        Damage = _Damage;
        MyRigidbody.velocity = _Movement.normalized * SpeedMultiplier;
        FiredVelocity = _Movement.normalized * SpeedMultiplier;
    }

    IEnumerator AllowDisappearance()
    {
        CanDisappear = false;
        yield return new WaitForSeconds(1);
        CanDisappear = true;
    }

    public void Rotate()
    {
        float _ResultRotationSpeed = Flipped ? RotationSpeed : -RotationSpeed;

        transform.Rotate(0, 0, _ResultRotationSpeed * Time.deltaTime);
    }

    #region Networking

    public void NetworkHitEnemy(Scr_Health_Enemy _HitEnemy, int _Damage)
    {
        //Debug.Log("Network hit " + _HitEnemy.name);

        if(_HitEnemy.gameObject.activeSelf)
        {
            _HitEnemy.TakeDamage(_Damage, Owner.gameObject);
            _HitEnemy.StartCoroutine(_HitEnemy.Master.DamagedSlowDown());
            _HitEnemy.SpillBlood(FiredVelocity);

        }

        //Check locally if the projectile pierce.
        if(!IsPierce)
        {
            gameObject.SetActive(false);
        }
    }

    public void SendData(long _Code = 0, byte[] _Data = null)
    {
        if(!NakamaConnection.Instance.IsMultiplayer())
        {
            return;
        }

        //switch(_Code)
        //{
        //    case OpCodes.AxeHit:
        //        NakamaConnection.Instance.RequestSendMatchState(_Data, _Code);
        //        break;
        //}
    }

    public void ReceiveData(IMatchState _MatchState)
    {
        switch (_MatchState.OpCode)
        {
            //case OpCodes.AxeHit:
            //    Dictionary<string, string> AxeHitData = MatchDataJSON.DecryptStateToDictionary(_MatchState.State);

            //    Scr_MainThreadDispatcher.Instance.Enqueue(() =>
            //    {
            //        //Debug.Log(transform.name + " / " + AxeHitData["AxeName"] + "\nAxeName check: " + (AxeHitData["AxeName"] == transform.name) + "\nOwner check: " + (AxeHitData["OwnerID"] == Owner.PlayerUsername));
            //        if (AxeHitData["AxeName"] == transform.name && AxeHitData["OwnerID"] == Owner.PlayerUsername)
            //        {
            //            Scr_Enemy HitEnemy = Scr_WaveSpawner.Instance.GetEnemy(AxeHitData["EnemyName"]);
            //            NetworkHitEnemy(HitEnemy.GetComponent<Scr_Health_Enemy>(), int.Parse(AxeHitData["Damage"]));
            //        }
            //    });

            //    break;
        }
    }

    public void AttachToNakama()
    {
        if (!NakamaConnection.Instance.IsMultiplayer() || NakamaConnection.Instance.IsLocalPlayer(Owner.PlayerUsername))
        {
            return;
        }
            
        NakamaConnection.Instance.AddToReceiver(ReceiveData);
    }

    #endregion
}
