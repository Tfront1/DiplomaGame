using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathFinderManager
{
    private static Dictionary<(Vector2, Vector2), (List<Vector2> Path, float TimeAdded)> _cachedPaths = new();
    private static Queue<PathRequest> _pathRequests = new();
    private static bool _isProcessingPath = false;

    private const float _pathCacheLifetime = 10f;
    private const float _distanceBetweenSamePath = 100f;
    
    public static event Action<Guid, List<Vector2>> OnPathFound;

    public static void RequestPath(Vector2 start, Vector2 end, Guid id, PathAction action)
    {
        _pathRequests.Enqueue(new PathRequest { Start = start, End = end, Id = id, Action = action });
        TryProcessNext();
    }

    private static void TryProcessNext()
    {
        if (!_isProcessingPath && _pathRequests.Count > 0)
        {
            _isProcessingPath = true;
            var request = _pathRequests.Dequeue();
            ProcessPathRequest(request);
        }
    }

    private static void ProcessPathRequest(PathRequest request)
    {
        ClearExpiredCacheEntries();

        List<Vector2> result;
        var isExistsPath = false;
        List<Vector2> foundRes = null;

        if (_cachedPaths.TryGetValue((request.Start, request.End), out var foundItem))
        {
            foundRes = foundItem.Path;
            isExistsPath = true;
        }
        else if (_cachedPaths.TryGetValue((request.End, request.Start), out var reverseFoundItem))
        {
            var reversedPath = new List<Vector2>(reverseFoundItem.Path);
            reversedPath.Reverse();
            foundRes = reversedPath;
            isExistsPath = true;
        }
        else
        {
            foreach (var cachedPath in _cachedPaths)
            {
                var cachedStart = cachedPath.Key.Item1;
                var cachedEnd = cachedPath.Key.Item2;

                if (Vector2.Distance(cachedStart, request.Start) <= _distanceBetweenSamePath)
                {
                    if (cachedEnd == request.End)
                    {
                        var reversePath = new List<Vector2>(cachedPath.Value.Path);
                        reversePath.Reverse();

                        foundRes = new List<Vector2>();

                        foreach (var point in reversePath)
                        {
                            if (!PathFinder.Instance.IsObstacleBetweenPoints(request.Start, point))
                            {
                                isExistsPath = true;
                                foundRes.Add(point);
                                break;
                            }
                            else
                            {
                                foundRes.Add(point);
                            }
                        }

                        foundRes.Reverse();
                        
                    }
                }
            }
        }

        if (isExistsPath)
        {
            result = foundRes;
        }
        else
        {
            if (request.Action == PathAction.MoveClose)
            {
                result = PathFinder.Instance.FindNearestAccessiblePath(request.Start, request.End);
            }
            else
            {
                result = PathFinder.Instance.FindPath(request.Start, request.End);
            }

            result.Remove(request.Start);
            _cachedPaths.Add((request.Start, request.End), (result, Time.time));
        }

        OnPathFound?.Invoke(request.Id, result);
        _isProcessingPath = false;
        TryProcessNext();
    }

    private static void ClearExpiredCacheEntries()
    {
        var currentTime = Time.time;
        var keysToRemove = new List<(Vector2, Vector2)>();

        foreach (var entry in _cachedPaths)
        {
            if (currentTime - entry.Value.TimeAdded > _pathCacheLifetime)
            {
                keysToRemove.Add(entry.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cachedPaths.Remove(key);
        }
    }

    public enum PathAction
    {
        MoveClose,
        Move,
        Refind
    }

    private struct PathRequest
    {
        public Guid Id;
        public Vector2 Start;
        public Vector2 End;
        public PathAction Action;
    }
}