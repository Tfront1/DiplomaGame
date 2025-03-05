using System;
using System.Collections;
using UnityEngine;

public class TickRateSystem : MonoBehaviour
{
    private static TickRateSystem _instance;
    public static TickRateSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("TickRateSystem");
                _instance = go.AddComponent<TickRateSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public event Action<float> OnTick;
    private float _ticksPerSecond = 2f;
    private Coroutine _tickCoroutine;
    private bool _isRunning = false;
    private float TickInterval => 1f / _ticksPerSecond;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        StartTicking();
    }

    private void OnDestroy()
    {
        StopTicking();
    }

    private IEnumerator TickRoutine()
    {
        var previousTime = Time.time;

        while (_isRunning)
        {
            yield return new WaitForSeconds(TickInterval);

            var currentTime = Time.time;
            var actualDeltaTime = currentTime - previousTime;
            previousTime = currentTime;

            OnTick?.Invoke(actualDeltaTime);
        }
    }

    private IEnumerator TickRoutineRealtime()
    {
        var previousTime = Time.realtimeSinceStartup;

        while (_isRunning)
        {
            yield return new WaitForSecondsRealtime(TickInterval);

            var currentTime = Time.realtimeSinceStartup;
            var actualDeltaTime = currentTime - previousTime;
            previousTime = currentTime;

            OnTick?.Invoke(actualDeltaTime);
        }
    }

    public void StartTicking()
    {
        if (_isRunning) return;

        _isRunning = true;
        _tickCoroutine = StartCoroutine(TickRoutine());
    }

    public void StopTicking()
    {
        if (!_isRunning) return;

        _isRunning = false;

        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }
    }

    public void SetTickRate(float ticksPerSecond)
    {
        if (ticksPerSecond <= 0)
        {
            Debug.LogWarning("Tick rate must be greater than 0. Setting to default 2 ticks per second.");
            _ticksPerSecond = 2f;
            return;
        }

        var wasRunning = _isRunning;

        if (wasRunning)
        {
            StopTicking();
        }

        _ticksPerSecond = ticksPerSecond;

        if (wasRunning)
        {
            StartTicking();
        }
    }
}
