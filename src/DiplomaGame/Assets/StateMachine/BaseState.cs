using GameUtilities.Utils;
using Selection;
using StateMachine.Interfaces;
using UnityEngine;

namespace StateMachine
{
    public class BaseState : IInputState
    {
        protected readonly InputStateMachine _stateMachine;
        
        public BaseState(InputStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public virtual void Enter()
        {

        }

        public virtual void Exit()
        {

        }
        
        public virtual void HandleRightClick(Vector2 position)
        {

        }

        public virtual void HandleLeftClick(Vector2 position)
        {
            _stateMachine.DeselectItems();
            var selectedItems = SelectorManager.Instance.GetSelectedItem(position);
            _stateMachine.HighlightSelectedItems(selectedItems.Item1);
            _stateMachine.SelectedItems = selectedItems.Item1;
            _stateMachine.ChangeState(selectedItems.Item2);
        }

        public virtual void HandleLeftHoldStart(Vector2 position)
        {
            _stateMachine.StartSelectPosition = position;

            if (_stateMachine.SelectionAreaVisual == null)
            {
                var selection = _stateMachine.CreateSelectionGameObject();
                _stateMachine.SelectionAreaVisual = selection.Item1;
                _stateMachine.SelectionRectTransform = selection.Item2;
            }

            _stateMachine.SelectionAreaVisual.SetActive(true);
            var selectionAreaPosition = new Vector3(position.x, position.y, -29f);
            _stateMachine.SelectionRectTransform.position = selectionAreaPosition;
            _stateMachine.SelectionRectTransform.sizeDelta = Vector2.zero;
        }

        public virtual void HandleLeftHold(Vector2 position)
        {
            if (_stateMachine.SelectionAreaVisual != null && _stateMachine.SelectionAreaVisual.activeSelf)
            {
                _stateMachine.UpdateSelectionRect(_stateMachine.StartSelectPosition, position);
            }
        }

        public void HandleLeftHoldEnd(Vector2 position)
        {
            if (_stateMachine.SelectionAreaVisual != null)
            {
                _stateMachine.SelectionAreaVisual.SetActive(false);
            }

            if (UtilsClass.CalculateDistance(position, _stateMachine.StartSelectPosition) > 1)
            {
                _stateMachine.DeselectItems();
                var selectedItems = SelectorManager.Instance.GetSelectedArea(
                    _stateMachine.StartSelectPosition, position, _stateMachine.IsSelectingUnitsOnly);
                _stateMachine.HighlightSelectedItems(selectedItems.Item1);
                _stateMachine.SelectedItems = selectedItems.Item1;
                _stateMachine.ChangeState(selectedItems.Item2);
            }
        }
    }
}