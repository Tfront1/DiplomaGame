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

        public BuildingActionUIController BuildingActionUI;

        public BuildingController(BuildingItem building)
        {
            _building = building;
            BuildingActionUI = _building.BuildingGameObject.AddComponent<BuildingActionUIController>();
            BuildingActionUI.Initialize(_building, this);
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
            BuildingActionUI.OpenActionPanel();
            SubscribeToClicks();
        }

        public void AddAction(Type action)
        {
            if (!BuildingActions.Contains(action))
            {
                BuildingActions.Add(action);
                BuildingActionUI.SetActions(BuildingActions);
            }
        }

        public void RemoveAction(Type action)
        {
            if (BuildingActions.Contains(action))
            {
                BuildingActions.Remove(action);
                BuildingActionUI.SetActions(BuildingActions);
            }
        }

        public void ClearActions()
        {
            BuildingActions.Clear();
            BuildingActionUI.SetActions(BuildingActions);
        }

        private void OnClick(Vector2 position)
        {
            BuildingActionUI.CloseActionPanel();
            UnsubscribeToClicks();
        }

        private void SubscribeToClicks()
        {
            GameplayInputHandler.Instance.CustomGraphicRaycasters.Add(BuildingActionUI.CanvasRaycaster);
            GameplayInputHandler.Instance.OnMouseLeftClick += OnClick;
            GameplayInputHandler.Instance.OnMouseRightClick += OnClick;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart += OnClick;
        }

        private void UnsubscribeToClicks()
        {
            GameplayInputHandler.Instance.CustomGraphicRaycasters.Remove(BuildingActionUI.CanvasRaycaster);
            GameplayInputHandler.Instance.OnMouseLeftClick -= OnClick;
            GameplayInputHandler.Instance.OnMouseRightClick -= OnClick;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart -= OnClick;
        }
    }
}
