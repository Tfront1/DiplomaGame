using System.Linq;
using Selection;
using UnityEngine;

namespace StateMachine.States
{
    public class BuildingSelectedState : BaseState
    {
        private BuildingItem _building;

        public BuildingSelectedState(InputStateMachine stateMachine): base(stateMachine)
        {

        }

        public override void Enter()
        {
            if (_stateMachine.SelectedItems.Count == 1)
            {
                _building = SelectorFactory.GetBuildingItem(_stateMachine.SelectedItems.First());

                _building.UIToChange += BuildingUIManager.Instance.UpdateBuildingInfo;
                _building.OnDestroyed += OnBuildingDestroyed;

                BuildingUIManager.Instance.ShowBuildingInfo(_building);
            }
             
            Debug.Log("Entering BuildingSelected state");
        }

        public override void Exit()
        {
            BuildingUIManager.Instance.HideBuildingInfo();
            Debug.Log("Exiting BuildingSelected state");
        }

        private void OnBuildingDestroyed(object sender, BuildingItem.BuildingDestroyedEventArgs args)
        {
            if (_building != null)
            {
                var building = args.BuildingItem;
                _stateMachine.SelectedItems.Remove(building);

                building.OnDestroyed -= OnBuildingDestroyed;
                building.UIToChange -= BuildingUIManager.Instance.UpdateBuildingInfo;

                _building = null;
            }
            Exit();
        }
    }
}