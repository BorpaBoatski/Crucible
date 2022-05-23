using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIdleSoundManager : MonoBehaviour
{
    [Header("Singleton")]
    public static EnemyIdleSoundManager Instance;

    bool CanPlaySkeleton = true;
    [SerializeField] float SkeletonInterval;

    bool CanPlayGhost = true;
    [SerializeField] float GhostInterval;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    IEnumerator SkeletonCountDown()
    {
        yield return new WaitForSecondsRealtime(SkeletonInterval);
        CanPlaySkeleton = true;
    }

    IEnumerator GhostCountDown()
    {
        yield return new WaitForSecondsRealtime(GhostInterval);
        CanPlayGhost = true;
    }

    public bool CheckIntervalPlayability(Enum_EnemyType _Type)
    {
        if (_Type == Enum_EnemyType.SKELETON)
        {
            if (CanPlaySkeleton)
            {
                CanPlaySkeleton = false;
                StartCoroutine(SkeletonCountDown());
                return true;
            }
        }
        else if (_Type == Enum_EnemyType.GHOST)
        {
            if (CanPlayGhost)
            {
                CanPlayGhost = false;
                StartCoroutine(GhostCountDown());
                return true;
            }
        }

        return false;
    }
}

