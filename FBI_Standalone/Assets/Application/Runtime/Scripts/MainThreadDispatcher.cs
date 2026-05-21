using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Queues actions from background threads and executes them on the Unity main thread in Update().
/// Add this component to a persistent GameObject (e.g. the same one as ConfigFileManager).
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private readonly Queue<Action> queue = new Queue<Action>();
    private readonly object lockObj = new object();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        lock (lockObj)
        {
            while (queue.Count > 0)
                queue.Dequeue().Invoke();
        }
    }

    /// <summary>
    /// Enqueue an action to run on the main thread next Update().
    /// Safe to call from any thread.
    /// </summary>
    public void Enqueue(Action action)
    {
        lock (lockObj)
            queue.Enqueue(action);
    }
}
