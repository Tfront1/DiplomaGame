using System;
using System.Collections;
using UnityEngine;

namespace UnitAction
{
    public class WaitUnitAction : BaseUnitAction
    {
        private float _msToWait;

        public WaitUnitAction(UnitItem unit, float msToWait) : base(unit)
        {
            _msToWait = msToWait;
        }

        public override bool CanExecute()
        {
            //ToDo: Add cases when unit can`t wait
            return true;
        }

        public override void Execute()
        {
            if (!CanExecute())
            {
                CompleteAction();
                return;
            }

            _idAction = Guid.NewGuid();
            CoroutineRunner.Instance.StartCoroutineWithId(_idAction, Wait(_msToWait));
        }

        private IEnumerator Wait(float msToWait)
        {
            var secondsToWait = msToWait / 1000f;

            var startTime = Time.time;
            var pausedTime = 0f;

            while (Time.time - startTime - pausedTime < secondsToWait)
            {
                if (IsStopped)
                {
                    yield break;
                }

                if (IsPaused)
                {
                    var timeWhenPaused = Time.time;
                    yield return new WaitUntil(() => !IsPaused);
                    pausedTime += (Time.time - timeWhenPaused);
                }

                yield return null;
            }

            CompleteAction();
        }
    }
}
