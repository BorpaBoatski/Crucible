using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class Scr_MainThreadDispatcher : MonoBehaviour
{
    public static Scr_MainThreadDispatcher Instance;

    [Header("Properties")]
    Queue<Action> PendingEvents = new Queue<Action>();

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock(PendingEvents)
        {
            while(PendingEvents.Count > 0)
            {
                PendingEvents.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action _NewAction)
    {
        lock(PendingEvents)
        {
            PendingEvents.Enqueue(_NewAction);
        }
    }

    private void OnApplicationQuit()
    {
        lock(PendingEvents)
        {
            PendingEvents.Clear();
        }
    }
}
