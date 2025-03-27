using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace StateMachine.States
{
    public class SupplySelectedState : BaseState
    {
        private SupplyItem _supply;

        public SupplySelectedState(InputStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            if (_stateMachine.SelectedItems.Count == 1)
            {
                _supply = _stateMachine.SelectedItems.First() as SupplyItem;
                if (_supply != null)
                {
                    SupplyUIManager.Instance.ShowSupplyInfo(_supply);
                    _supply.UIToChange += SupplyUIManager.Instance.UpdateSupplyInfo;
                    _supply.OnDestroyed += OnSupplyDestroyed;
                }
            }

            Debug.Log("Entering SupplySelected state");
        }

        public override void Exit()
        {
            if (_supply != null && !_supply.IsDestroyed())
            {
                _supply.UIToChange -= SupplyUIManager.Instance.UpdateSupplyInfo;
            }

            SupplyUIManager.Instance.HideSupplyInfo();
            _supply = null;
            Debug.Log("Exiting SupplySelected state");
        }

        private void OnSupplyDestroyed(object sender, SupplyItem.SupplyDestroyedEventArgs args)
        {
            _stateMachine.SelectedItems.Remove(_supply);
            _supply.OnDestroyed -= OnSupplyDestroyed;
            _supply.UIToChange -= SupplyUIManager.Instance.UpdateSupplyInfo;
            Exit();
        }
    }
}