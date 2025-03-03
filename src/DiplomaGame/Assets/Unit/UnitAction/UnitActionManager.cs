using System;
using System.Collections.Generic;
using System.Linq;

public class UnitActionManager
{
    private Dictionary<Guid, Queue<IUnitAction>> _unitActionQueues = new();
    private Dictionary<Guid, IUnitAction> _currentActions = new();
    private bool _isPaused = false;

    public bool IsPaused => _isPaused;

    public void QueueAction(IUnitAction action)
    {
        var unitGuid = action.GetUnit().GetGuid();

        if (!_currentActions.ContainsKey(unitGuid))
        {
            if (action.CanExecute())
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
        }

        if (!_unitActionQueues.ContainsKey(unitGuid))
        {
            _unitActionQueues[unitGuid] = new Queue<IUnitAction>();
        }
        _unitActionQueues[unitGuid].Enqueue(action);
    }

    public void QueueActions(UnitItem unit, List<IUnitAction> actions)
    {
        foreach (var action in actions)
        {
            QueueAction(action);
        }
    }

    public void PauseAllActions()
    {
        if (_isPaused) return;

        _isPaused = true;

        foreach (var action in _currentActions.Values)
        {
            action.Pause();
        }
    }

    public void ResumeAllActions()
    {
        if (!_isPaused) return;

        _isPaused = false;

        foreach (var action in _currentActions.Values)
        {
            action.Resume();
        }
    }

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

    private void HandleActionCompleted(IUnitAction action)
    {
        action.OnActionCompleted -= HandleActionCompleted;

        var unitGuid = action.GetUnit().GetGuid();
        if (_currentActions.ContainsKey(unitGuid) && _currentActions[unitGuid] == action)
        {
            _currentActions.Remove(unitGuid);

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

    public bool ExecuteImmediately(IUnitAction action)
    {
        var unitGuid = action.GetUnit().GetGuid();

        if (_currentActions.ContainsKey(unitGuid))
        {
            var currentAction = _currentActions[unitGuid];
            currentAction.Cancel();
            currentAction.OnActionCompleted -= HandleActionCompleted;
            _currentActions.Remove(unitGuid);
        }

        if (action.CanExecute())
        {
            action.OnActionCompleted += HandleActionCompleted;
            _currentActions[unitGuid] = action;

            if (_isPaused)
            {
                action.Pause();
            }

            action.Execute();
            return true;
        }
        return false;
    }

    public void InterruptCurrentAction(UnitItem unit)
    {
        var unitGuid = unit.GetGuid();

        if (_currentActions.ContainsKey(unitGuid))
        {
            var currentAction = _currentActions[unitGuid];
            currentAction.Cancel();
            currentAction.OnActionCompleted -= HandleActionCompleted;
            _currentActions.Remove(unitGuid);
        }

        if (_unitActionQueues.ContainsKey(unitGuid))
        {
            _unitActionQueues[unitGuid].Clear();
        }
    }

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

    public bool IsUnitBusy(UnitItem unit)
    {
        var unitGuid = unit.GetGuid();
        return _currentActions.ContainsKey(unitGuid) || (_unitActionQueues.ContainsKey(unitGuid) && _unitActionQueues[unitGuid].Count > 0);
    }

    public int GetQueuedActionCount(UnitItem unit)
    {
        var unitGuid = unit.GetGuid();
        if (!_unitActionQueues.ContainsKey(unitGuid))
        {
            return 0;
        }
        return _unitActionQueues[unitGuid].Count;
    }
}