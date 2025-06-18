using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bots
{
    public class BotBrainManager : MonoBehaviour
    {
        private static BotBrainManager _instance;
        public static BotBrainManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("BotBrainManager");
                    _instance = go.AddComponent<BotBrainManager>();
                }
                return _instance;
            }
        }

        private readonly List<BotBrain> _activeBots = new();
        private readonly object _lock = new();
        private bool _isProcessing = false;
        private bool _isPaused = false;
        private float _thinkInterval = 1f;
        private Coroutine _processingCoroutine;

        public void PauseBots()
        {
            _isPaused = true;
        }

        public void ResumeBots()
        {
            _isPaused = false;
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
        }

        public bool IsPaused => _isPaused;

        public void RegisterBot(BotBrain botBrain)
        {
            lock (_lock)
            {
                if (!_activeBots.Contains(botBrain))
                {
                    _activeBots.Add(botBrain);
                    if (!botBrain.IsSortedSupplies)
                    {
                        botBrain.SortNearestSupplies();
                    }

                    if (!botBrain.IsSortedEnemyTowns)
                    {
                        botBrain.SortNearestEnemyTowns();
                    }

                    if (!_isProcessing)
                    {
                        StartProcessing();
                    }
                }
            }
        }

        public void UnregisterBot(BotBrain botBrain)
        {
            lock (_lock)
            {
                if (_activeBots.Remove(botBrain))
                {
                    if (_activeBots.Count == 0)
                    {
                        StopProcessing();
                    }
                }
            }
        }

        private void StartProcessing()
        {
            _isProcessing = true;
            if (_processingCoroutine != null)
            {
                StopCoroutine(_processingCoroutine);
            }
            _processingCoroutine = StartCoroutine(ProcessBotsCoroutine());
        }

        private void StopProcessing()
        {
            _isProcessing = false;
            if (_processingCoroutine != null)
            {
                StopCoroutine(_processingCoroutine);
                _processingCoroutine = null;
            }
        }

        private IEnumerator ProcessBotsCoroutine()
        {
            while (_isProcessing)
            {
                if (_isPaused)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                List<BotBrain> botsToProcess = null;
                lock (_lock)
                {
                    if (_activeBots.Count > 0)
                    {
                        botsToProcess = new List<BotBrain>(_activeBots);
                    }
                }

                if (botsToProcess != null)
                {
                    var threadCount = ThreadPoolManager.Instance.ThreadCount;
                    var botsPerThread = Mathf.CeilToInt((float)botsToProcess.Count / threadCount);
                    var completedThreads = 0;
                    var completionLock = new object();

                    for (var i = 0; i < threadCount; i++)
                    {
                        var startIndex = i * botsPerThread;
                        var endIndex = Mathf.Min(startIndex + botsPerThread, botsToProcess.Count);

                        if (startIndex >= botsToProcess.Count)
                            break;

                        ThreadPoolManager.Instance.QueueJob(() =>
                        {
                            try
                            {
                                for (var j = startIndex; j < endIndex; j++)
                                {
                                    if (!_isProcessing || _isPaused) break;
                                    if (botsToProcess[j] != null)
                                    {
                                        botsToProcess[j].Think();
                                    }
                                }
                            }
                            finally
                            {
                                lock (completionLock)
                                {
                                    completedThreads++;
                                }
                            }
                        });
                    }

                    yield return new WaitUntil(() =>
                    {
                        lock (completionLock)
                        {
                            return completedThreads >= Mathf.Min(threadCount,
                                Mathf.CeilToInt((float)botsToProcess.Count / botsPerThread));
                        }
                    });
                }

                yield return new WaitForSeconds(_thinkInterval);
            }
        }

        public void SetThinkInterval(float interval)
        {
            _thinkInterval = interval;
        }

        public int ActiveBotsCount
        {
            get
            {
                lock (_lock)
                {
                    return _activeBots.Count;
                }
            }
        }

        public void StopAllBots()
        {
            lock (_lock)
            {
                _activeBots.Clear();
            }
            StopProcessing();
        }

        private void OnDestroy()
        {
            StopAllBots();
        }
    }
}