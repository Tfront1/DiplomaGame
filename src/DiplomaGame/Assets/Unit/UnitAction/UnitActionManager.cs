using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages execution, queueing, and lifecycle of unit actions in a game or simulation system.
/// Handles action priority, interruption, and state control (pause/resume).
/// </summary>
public class UnitActionManager
{
    private static UnitActionManager _instance;
    private static readonly object _lock = new();

    public static UnitActionManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new UnitActionManager();
                }
                return _instance;
            }
        }
    }

    // Stores queued actions for each unit (by unit GUID)
    private Dictionary<Guid, Queue<IUnitAction>> _unitActionQueues = new();

    // Tracks currently executing actions for each unit
    private Dictionary<Guid, IUnitAction> _currentActions = new();

    // Global pause state for all actions
    private bool _isPaused = false;

    /// <summary>
    /// Gets whether the action manager is currently in a paused state.
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Adds an action to a unit's queue, executing it immediately if possible.
    /// Actions are queued when the unit is already performing another action.
    /// </summary>
    /// <param name="action">The action to queue or execute</param>
    public void QueueAction(IUnitAction action)
    {
        var unitGuid = action.GetUnit().GetId();

        // If unit isn't busy and action can execute, run it immediately
        if (!_currentActions.ContainsKey(unitGuid))
        {
            action.OnActionCompleted += HandleActionCompleted;
            _currentActions[unitGuid] = action;

            if (_isPaused)
            {
                action.Pause();
            }

            action.Execute();
            return;
        }

        // Otherwise, add to queue for later execution
        if (!_unitActionQueues.ContainsKey(unitGuid))
        {
            _unitActionQueues[unitGuid] = new Queue<IUnitAction>();
        }
        _unitActionQueues[unitGuid].Enqueue(action);
    }

    /// <summary>
    /// Convenience method to queue multiple actions for a single unit.
    /// </summary>
    /// <param name="unit">The unit to queue actions for</param>
    /// <param name="actions">List of actions to queue</param>
    public void QueueActions(UnitItem unit, List<IUnitAction> actions)
    {
        foreach (var action in actions)
        {
            QueueAction(action);
        }
    }

    /// <summary>
    /// Pauses all currently executing actions system-wide.
    /// Has no effect if already paused.
    /// </summary>
    public void PauseAllActions()
    {
        if (_isPaused) return;

        _isPaused = true;

        foreach (var action in _currentActions.Values)
        {
            action.Pause();
        }
    }

    /// <summary>
    /// Resumes all currently paused actions system-wide.
    /// Has no effect if not paused.
    /// </summary>
    public void ResumeAllActions()
    {
        if (!_isPaused) return;

        _isPaused = false;

        foreach (var action in _currentActions.Values)
        {
            action.Resume();
        }
    }

    /// <summary>
    /// Toggles between paused and resumed states for all actions.
    /// </summary>
    public void TogglePause()
    {
        if (_isPaused)
        {
            ResumeAllActions();
        }
        else
        {
            PauseAllActions();
        }
    }

    /// <summary>
    /// Callback handler for action completion events.
    /// Removes completed actions and starts the next queued action if available.
    /// </summary>
    /// <param name="action">The action that completed</param>
    private void HandleActionCompleted(IUnitAction action)
    {
        action.OnActionCompleted -= HandleActionCompleted;

        var unitGuid = action.GetUnit().GetId();
        if (_currentActions.ContainsKey(unitGuid) && _currentActions[unitGuid] == action)
        {
            _currentActions.Remove(unitGuid);

            // Start next action in queue if one exists
            if (_unitActionQueues.ContainsKey(unitGuid) && _unitActionQueues[unitGuid].Count > 0)
            {
                var nextAction = _unitActionQueues[unitGuid].Dequeue();
                nextAction.OnActionCompleted += HandleActionCompleted;
                _currentActions[unitGuid] = nextAction;

                if (_isPaused)
                {
                    nextAction.Pause();
                }

                nextAction.Execute();
            }
        }
    }

    /// <summary>
    /// Starts execution of all queued actions for units that aren't currently busy.
    /// Useful for processing initial action queues or after clearing actions.
    /// </summary>
    public void ExecuteAllActions()
    {
        foreach (var unitGuid in _unitActionQueues.Keys.ToList())
        {
            if (!_currentActions.ContainsKey(unitGuid) && _unitActionQueues[unitGuid].Count > 0)
            {
                var nextAction = _unitActionQueues[unitGuid].Dequeue();
                nextAction.OnActionCompleted += HandleActionCompleted;
                _currentActions[unitGuid] = nextAction;

                if (_isPaused)
                {
                    nextAction.Pause();
                }

                nextAction.Execute();
            }
        }
    }

    /// <summary>
    /// Immediately executes an action, canceling the current action and clearing the action queue for that unit.
    /// </summary>
    /// <param name="action">The action to execute immediately</param>
    /// <returns>True if the action could be executed, false otherwise</returns>
    public bool ExecuteImmediately(IUnitAction action)
    {
        var unitGuid = action.GetUnit().GetId();

        // Cancel the current action if one exists
        if (_currentActions.TryGetValue(unitGuid, out var currentAction))
        {
            currentAction.Cancel();
        }

        // Clear any queued actions
        if (_unitActionQueues.ContainsKey(unitGuid))
        {
            foreach (var unitAction in _unitActionQueues[unitGuid])
            {
                unitAction.Cancel();
            }
            _unitActionQueues[unitGuid].Clear();
        }

        QueueAction(action);
        return true;
    }

    /// <summary>
    /// Stops the current action and clears the action queue for a specific unit.
    /// Used for emergency interruptions or when changing unit states.
    /// </summary>
    /// <param name="unit">The unit whose actions should be interrupted</param>
    public void InterruptCurrentAction(UnitItem unit)
    {
        var unitGuid = unit.GetId();

        // Cancel current action if one exists
        if (_currentActions.ContainsKey(unitGuid))
        {
            var currentAction = _currentActions[unitGuid];
            currentAction.Cancel();
            currentAction.OnActionCompleted -= HandleActionCompleted;
            _currentActions.Remove(unitGuid);
        }

        // Clear any queued actions
        if (_unitActionQueues.ContainsKey(unitGuid))
        {
            _unitActionQueues[unitGuid].Clear();
        }
    }

    /// <summary>
    /// Cancels all currently executing actions and clears all action queues.
    /// Used for global reset or when changing game states.
    /// </summary>
    public void ClearAllActionQueues()
    {
        foreach (var action in _currentActions.Values)
        {
            action.Cancel();
            action.OnActionCompleted -= HandleActionCompleted;
        }

        _unitActionQueues.Clear();
        _currentActions.Clear();
    }

    /// <summary>
    /// Checks if a unit is currently executing an action or has actions queued.
    /// </summary>
    /// <param name="unit">The unit to check</param>
    /// <returns>True if the unit is busy, false if idle</returns>
    public bool IsUnitBusy(UnitItem unit)
    {
        var unitGuid = unit.GetId();
        return _currentActions.ContainsKey(unitGuid) || (_unitActionQueues.ContainsKey(unitGuid) && _unitActionQueues[unitGuid].Count > 0);
    }

    /// <summary>
    /// Returns the number of actions currently queued for a unit.
    /// Does not include the currently executing action.
    /// </summary>
    /// <param name="unit">The unit to check</param>
    /// <returns>The number of queued actions</returns>
    public int GetQueuedActionCount(UnitItem unit)
    {
        var unitGuid = unit.GetId();
        if (!_unitActionQueues.ContainsKey(unitGuid))
        {
            return 0;
        }
        return _unitActionQueues[unitGuid].Count;
    }

    /// <summary>
    /// Returns the currently executing action.
    /// </summary>
    /// <param name="unit">The unit to check</param>
    /// <returns>The currently executing action for unit</returns>
    public IUnitAction GetCurrentUnitAction(UnitItem unit)
    {
        var unitGuid = unit.GetId();
        return _currentActions.GetValueOrDefault(unitGuid);
    }
}