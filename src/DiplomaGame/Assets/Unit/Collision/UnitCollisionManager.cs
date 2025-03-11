using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCollisionManager : MonoBehaviour
{
    private static UnitCollisionManager _instance;

    // Stores the last update timestamp for each unit
    private Dictionary<Guid, float> _lastUpdateTimes = new();

    // Dictionary to store position updates with unit ID as key
    private Dictionary<Guid, PositionUpdateRequest> _updateDictionary = new();

    // List to control processing order
    private List<Guid> _processingOrder = new();

    // Time interval between allowed updates for the same unit (1 second)
    private const float UpdateInterval = 0.5f;

    private UnitCollisionSystem _collisionSystem = new();

    public static UnitCollisionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitCollisionManager>();

                if (_instance == null)
                {
                    var managerObject = new GameObject("TownUnitPositionManager");
                    _instance = managerObject.AddComponent<UnitCollisionManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(ProcessQueueRoutine());
    }

    /// <summary>
    /// Requests a position update for a unit with throttling
    /// </summary>
    /// <param name="unit">The unit to update</param>
    /// <param name="oldPosition">The previous position</param>
    /// <param name="newPosition">The new position</param>
    public void RequestPositionUpdate(UnitItem unit, Vector2 oldPosition, Vector2 newPosition)
    {
        if (unit == null || unit.HomeTown == null) return;

        var request = new PositionUpdateRequest
        {
            Unit = unit,
            OldPosition = oldPosition,
            NewPosition = newPosition,
        };

        // Simply replace the existing request or add a new one
        if (!_updateDictionary.ContainsKey(unit.Id))
        {
            _processingOrder.Add(unit.Id);
        }

        _updateDictionary[unit.Id] = request;
    }

    private IEnumerator ProcessQueueRoutine()
    {
        while (true)
        {
            if (_processingOrder.Count > 0)
            {
                ProcessNextInQueue();
            }

            yield return null;
        }
    }

    private void ProcessNextInQueue()
    {
        var unitId = _processingOrder[0];
        _processingOrder.RemoveAt(0);

        if (!_updateDictionary.TryGetValue(unitId, out var request))
        {
            return;
        }

        var unit = request.Unit;

        if (unit == null || unit.HomeTown == null)
        {
            _updateDictionary.Remove(unitId);
            return;
        }

        if (IsUnitEligibleForUpdate(unit.Id))
        {
            _lastUpdateTimes[unit.Id] = Time.time;

            _collisionSystem.UpdateUnitPosition(
                unit,
                request.OldPosition,
                request.NewPosition
            );
        }
        else
        {
            // If not eligible now, add back to the end of processing order for later processing
            _processingOrder.Add(unitId);
        }

        _updateDictionary.Remove(unitId);
    }

    private bool IsUnitEligibleForUpdate(Guid unitId)
    {
        if (!_lastUpdateTimes.TryGetValue(unitId, out var lastUpdateTime))
        {
            return true;
        }

        return (Time.time - lastUpdateTime) >= UpdateInterval;
    }

    /// <summary>
    /// Clears a unit from the update tracking when it's removed from the game
    /// </summary>
    public void RemoveUnitTracking(UnitItem unit)
    {
        if (unit != null)
        {
            if (_lastUpdateTimes.ContainsKey(unit.Id))
            {
                _lastUpdateTimes.Remove(unit.Id);
            }

            if (_updateDictionary.ContainsKey(unit.Id))
            {
                _updateDictionary.Remove(unit.Id);
                _processingOrder.Remove(unit.Id);
            }

            _collisionSystem.UnregisterUnit(unit);
        }
    }

    private struct PositionUpdateRequest
    {
        public UnitItem Unit;
        public Vector2 OldPosition;
        public Vector2 NewPosition;
    }
}