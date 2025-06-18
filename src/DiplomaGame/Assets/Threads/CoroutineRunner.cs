using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    private Dictionary<Guid, CoroutineInfo> _activeCoroutines = new();
    
    private struct CoroutineInfo
    {
        public Coroutine Wrapper;
        public Coroutine Original;
        public bool IsRunning;
    }

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                _instance = go.AddComponent<CoroutineRunner>();
            }
            return _instance;
        }
    }

    public Coroutine StartCoroutineWithId(Guid id, IEnumerator routine)
    {
        StopCoroutineWithId(id);

        try
        {
            var original = StartCoroutine(routine);

            var wrapper = StartCoroutine(WrapCoroutine(id, original));

            _activeCoroutines[id] = new CoroutineInfo
            {
                Wrapper = wrapper,
                Original = original,
                IsRunning = true
            };

            return original;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error starting coroutine with id {id}: {e.Message}");
            return null;
        }
    }

    private IEnumerator WrapCoroutine(Guid id, Coroutine original)
    {
        yield return original;

        if (_activeCoroutines.TryGetValue(id, out var info))
        {
            info.IsRunning = false;
            _activeCoroutines[id] = info;

            yield return null;

            if (_activeCoroutines.ContainsKey(id))
            {
                _activeCoroutines.Remove(id);
            }
        }
    }

    public void StopCoroutineWithId(Guid id)
    {
        if (_activeCoroutines.TryGetValue(id, out var info))
        {
            try
            {
                if (info.Original != null)
                    StopCoroutine(info.Original);

                if (info.Wrapper != null)
                    StopCoroutine(info.Wrapper);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error stopping coroutine {id}: {e.Message}");
            }
            finally
            {
                _activeCoroutines.Remove(id);
            }
        }
    }

    public bool IsCoroutineRunning(Guid id)
    {
        return _activeCoroutines.TryGetValue(id, out var info) && info.IsRunning;
    }

    public new void StopAllCoroutines()
    {
        base.StopAllCoroutines();
        _activeCoroutines.Clear();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        _activeCoroutines.Clear();
    }
}