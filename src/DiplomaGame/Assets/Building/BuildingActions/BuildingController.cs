using System;
using System.Collections.Generic;
using Assets.Items.Interfaces;
using UnityEngine;

namespace BuildingAction
{
    public class BuildingController
    {
        private BuildingItem _building;
        public List<Type> BuildingActions { get; set; } = new();
        public List<UnitItem> SelectedUnits { get; set; }
        public IBackpackItem Item { get; set; } = null;

        private BuildingActionUIController _buildingActionUI;

        public BuildingController(BuildingItem building)
        {
            _building = building;
            _buildingActionUI = _building.BuildingGameObject.AddComponent<BuildingActionUIController>();
            _buildingActionUI.Initialize(_building, this);
        }

        public void ExecuteAction(Type actionType)
        {
            if (BuildingActions.Contains(actionType))
            {
                var action = BuildingActionFabric.CreateAction(actionType, SelectedUnits, Item);
                action.Execute(_building);
                UnsubscribeToClicks();
                Item = null;
            }
        }

        public void OpenActionPanel(List<UnitItem> selectedUnits)
        {
            SelectedUnits = selectedUnits;
            _buildingActionUI.OpenActionPanel();
            SubscribeToClicks();
        }

        public void AddAction(Type action)
        {
            if (!BuildingActions.Contains(action))
            {
                BuildingActions.Add(action);
                _buildingActionUI.SetActions(BuildingActions);
            }
        }

        public void RemoveAction(Type action)
        {
            if (BuildingActions.Contains(action))
            {
                BuildingActions.Remove(action);
                _buildingActionUI.SetActions(BuildingActions);
            }
        }

        private void OnClick(Vector2 position)
        {
            _buildingActionUI.CloseActionPanel();
            UnsubscribeToClicks();
        }

        private void SubscribeToClicks()
        {
            GameplayInputHandler.Instance.CustomGraphicRaycasters.Add(_buildingActionUI.CanvasRaycaster);
            GameplayInputHandler.Instance.OnMouseLeftClick += OnClick;
            GameplayInputHandler.Instance.OnMouseRightClick += OnClick;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart += OnClick;
        }

        private void UnsubscribeToClicks()
        {
            GameplayInputHandler.Instance.CustomGraphicRaycasters.Remove(_buildingActionUI.CanvasRaycaster);
            GameplayInputHandler.Instance.OnMouseLeftClick -= OnClick;
            GameplayInputHandler.Instance.OnMouseRightClick -= OnClick;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart -= OnClick;
        }
    }
}
