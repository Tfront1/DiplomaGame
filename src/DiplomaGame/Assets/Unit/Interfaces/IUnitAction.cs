using System;

public interface IUnitAction
{
    void Execute();

    bool CanExecute();

    UnitItem GetUnit();

    event Action<IUnitAction> OnActionCompleted;
    bool IsPaused { get; }
    void Pause();
    void Resume();
    void Cancel();
}