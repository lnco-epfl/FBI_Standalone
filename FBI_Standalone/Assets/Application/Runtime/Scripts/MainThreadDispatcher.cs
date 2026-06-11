using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private readonly Queue<Action> queue = new Queue<Action>();
    private readonly object lockObj = new object();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        lock (lockObj)
        {
            while (queue.Count > 0)
                queue.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        lock (lockObj)
            queue.Enqueue(action);
    }
}
