using System;

namespace UnitAction
{
    public abstract class BaseUnitAction : IUnitAction
    {
        protected Guid _idAction;

        protected UnitItem _unit;
        public event Action<IUnitAction> OnActionCompleted;

        protected bool _isPaused = false;
        public bool IsPaused => _isPaused;

        protected bool _isStopped = false;
        public bool IsStopped => _isStopped;

        protected bool _isSuccessAction = false;
        public bool IsSuccess => _isSuccessAction;

        public BaseUnitAction(UnitItem unit)
        {
            _unit = unit;
        }

        public abstract void Execute();

        public abstract bool CanExecute();

        public UnitItem GetUnit()
        {
            return _unit;
        }

        protected virtual void CompleteAction()
        {
            OnActionCompleted?.Invoke(this);
        }


        public virtual void Pause()
        {
            _isPaused = true;
        }

        public virtual void Resume()
        {
            _isPaused = false;
        }

        public virtual void Cancel()
        {
            _isStopped = true;
        }
    }
}
