using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class ThreadPoolManager : MonoBehaviour
{
    private static ThreadPoolManager _instance;

    public static ThreadPoolManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    var go = new GameObject("ThreadPoolManager");
                    _instance = go.AddComponent<ThreadPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
    }

    private ConcurrentQueue<Action> threadJobs = new();
    private readonly ConcurrentQueue<Action> mainThreadActions = new();
    private volatile bool isRunning = true;
    private int _activeJobs = 0;
    private ManualResetEventSlim _jobSignal = new(false);
    private Thread[] _threads;

    private static object _lock = new();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        var threadCount = Mathf.Max(1, SystemInfo.processorCount - 1);
        _threads = new Thread[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            _threads[i] = new Thread(ThreadLoop);
            _threads[i].IsBackground = true;
            _threads[i].Name = $"Worker-{i}";
            _threads[i].Start();
        }

        Debug.Log($"ThreadPoolManager initialized with {threadCount} threads");
    }

    public void QueueJob(Action job)
    {
        if (job == null) return;

        Interlocked.Increment(ref _activeJobs);
        threadJobs.Enqueue(job);
        _jobSignal.Set();
    }

    public void ExecuteOnMainThread(Action action)
    {
        if (action == null) return;
        mainThreadActions.Enqueue(action);
    }

    public void QueueJobWithCallback(Action backgroundJob, Action mainThreadCallback)
    {
        QueueJob(() => {
            try
            {
                backgroundJob?.Invoke();
            }
            finally
            {
                if (mainThreadCallback != null)
                    ExecuteOnMainThread(mainThreadCallback);
            }
        });
    }

    public void QueueJobWithResult<T>(Func<T> backgroundJob, Action<T> mainThreadCallback)
    {
        QueueJob(() => {
            T result = default;
            try
            {
                result = backgroundJob.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Thread job exception: {e}");
            }
            finally
            {
                if (mainThreadCallback != null)
                    ExecuteOnMainThread(() => mainThreadCallback(result));
            }
        });
    }

    private void ThreadLoop()
    {
        while (isRunning)
        {
            _jobSignal.Wait();

            while (threadJobs.TryDequeue(out var job))
            {
                try
                {
                    job.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Thread job exception: {e}");
                }
                finally
                {
                    Interlocked.Decrement(ref _activeJobs);
                }

                if (!isRunning) break;
            }

            if (_activeJobs == 0 || !isRunning)
            {
                _jobSignal.Reset();
            }
        }
    }

    private void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Main thread action exception: {e}");
            }
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
        _jobSignal.Set();

        if (_threads != null)
        {
            foreach (var thread in _threads)
            {
                if (thread != null && thread.IsAlive)
                {
                    thread.Join(100);
                }
            }
        }

        _jobSignal.Dispose();
    }

    public int ActiveJobsCount => _activeJobs;
    public int QueuedJobsCount => threadJobs.Count;
}