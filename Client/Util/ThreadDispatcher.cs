using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspConnected.Client.Util
{
    /// <summary>
    /// Allows background threads to schedule work on Unity's main thread.
    /// Background threads call Enqueue(action); Unity's Update() drains it.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ThreadDispatcher : MonoBehaviour
    {
        public static ThreadDispatcher Instance { get; private set; }

        private readonly Queue<Action> _queue = new Queue<Action>();
        private readonly object _lock = new object();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Enqueue(Action action)
        {
            lock (_lock)
                _queue.Enqueue(action);
        }

        private void Update()
        {
            int limit = 50; // max actions to drain per frame
            while (limit-- > 0)
            {
                Action action;
                lock (_lock)
                {
                    if (_queue.Count == 0) break;
                    action = _queue.Dequeue();
                }
                try { action(); }
                catch (Exception ex) { KspLog.Error("ThreadDispatcher action threw: " + ex); }
            }
        }
    }
}
