using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    private Dictionary<Guid, Coroutine> _activeCoroutines = new();

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                _instance = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public Coroutine StartCoroutineWithId(Guid id, IEnumerator routine)
    {
        if (_activeCoroutines.ContainsKey(id))
        {
            StopCoroutine(_activeCoroutines[id]);
            _activeCoroutines.Remove(id);
        }

        var coroutine = StartCoroutine(WrapCoroutine(id, routine));
        _activeCoroutines[id] = coroutine;
        return coroutine;
    }

    private IEnumerator WrapCoroutine(Guid id, IEnumerator routine)
    {
        yield return StartCoroutine(routine);

        if (_activeCoroutines.ContainsKey(id))
        {
            _activeCoroutines.Remove(id);
        }
    }

    public void StopCoroutineWithId(Guid id)
    {
        if (_activeCoroutines.ContainsKey(id))
        {
            StopCoroutine(_activeCoroutines[id]);
            _activeCoroutines.Remove(id);
        }
        else
        {
            _activeCoroutines.Remove(id);
        }
    }
    
    public new void StopAllCoroutines()
    {
        base.StopAllCoroutines();
        _activeCoroutines.Clear();
    }
}