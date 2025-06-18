using System.Collections.Generic;
using Selection.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using StateMachine.Interfaces;
using StateMachine.States;

namespace StateMachine
{
    public class InputStateMachine : MonoBehaviour
    {
        private static InputStateMachine _instance;
        private static readonly object _lock = new();

        public bool IsSelectingUnitsOnly { get; set; } = true;
        public HashSet<ISelectable> SelectedItems { get; set; } = new();
        public GameObject SelectionAreaVisual { get; set; }
        public RectTransform SelectionRectTransform { get; set; }
        public Vector2 StartSelectPosition { get; set; }

        private Vector3 _selectionAreaPosition = new();

        private IInputState _currentState;

        private Dictionary<SelectedStates, IInputState> _states;
        private Canvas canvas = null;
        public bool IsShiftHold { get; private set; } = false;

        public static InputStateMachine Instance
        {
            get
            {
                lock (_lock)
                {
                    var instances = FindObjectsOfType<InputStateMachine>();
                    if (instances.Length > 0)
                    {
                        _instance = instances[0];
                        if (instances.Length > 1)
                        {
                            Debug.LogWarning("Found multiple InputStateMachine instances in scene. Using the first one.");
                            for (var i = 1; i < instances.Length; i++)
                            {
                                Destroy(instances[i].gameObject);
                            }
                        }
                    }
                    else
                    {
                        var go = new GameObject("InputStateMachine");
                        _instance = go.AddComponent<InputStateMachine>();
                    }
                    return _instance;
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            InitializeStates();

            GameplayInputHandler.Instance.OnMouseLeftClick += OnLeftClick;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart += OnLeftHoldStart;
            GameplayInputHandler.Instance.OnMouseLeftHoldEnd += OnLeftHoldEnd;
            GameplayInputHandler.Instance.OnMouseLeftHold += OnLeftHold;

            GameplayInputHandler.Instance.OnMouseRightClick += OnRightClick;

            UIInputHandler.Instance.OnShiftStart += OnShiftStart;
            UIInputHandler.Instance.OnShiftEnd += OnShiftEnd;

            ChangeState(SelectedStates.NothingSelect);
        }

        private void InitializeStates()
        {
            _states = new Dictionary<SelectedStates, IInputState>
            {
                { SelectedStates.NothingSelect, new NothingSelectedState(this) },
                { SelectedStates.UnitSelect, new UnitSelectedState(this) },
                { SelectedStates.BuildingSelect, new BuildingSelectedState(this) },
                { SelectedStates.SupplySelect, new SupplySelectedState(this) }
            };
        }

        private void OnDestroy()
        {
            GameplayInputHandler.Instance.OnMouseLeftClick -= OnLeftClick;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart -= OnLeftHoldStart;
            GameplayInputHandler.Instance.OnMouseLeftHoldEnd -= OnLeftHoldEnd;
            GameplayInputHandler.Instance.OnMouseLeftHold -= OnLeftHold;

            GameplayInputHandler.Instance.OnMouseRightClick -= OnRightClick;

            UIInputHandler.Instance.OnShiftStart -= OnShiftStart;
            UIInputHandler.Instance.OnShiftEnd -= OnShiftEnd;
        }

        private void OnLeftClick(Vector2 position) => _currentState.HandleLeftClick(position);
        private void OnLeftHoldStart(Vector2 position) => _currentState.HandleLeftHoldStart(position);
        private void OnLeftHold(Vector2 position) => _currentState.HandleLeftHold(position);
        private void OnLeftHoldEnd(Vector2 position) => _currentState.HandleLeftHoldEnd(position);
        private void OnRightClick(Vector2 position) => _currentState.HandleRightClick(position);

        public void ChangeState(SelectedStates newState)
        {
            _currentState?.Exit();

            if (_states.TryGetValue(newState, out var state))
            {
                _currentState = state;
                _currentState.Enter();
            }
            else
            {
                Debug.LogError($"State {newState} not found!");
            }
        }

        public void DeselectItems()
        {
            foreach (var item in SelectedItems)
            {
                if(!item.IsDestroyed)
                    item.OnDeselect();
            }
            SelectedItems.Clear();
        }

        public void DeselectItem(ISelectable item)
        {
            item.OnDeselect();
            SelectedItems.Remove(item);
        }


        public void HighlightSelectedItems(HashSet<ISelectable> items)
        {
            foreach (var item in items)
            {
                if (!item.IsDestroyed)
                {
                    item.OnSelect();
                }
            }
        }

        public (GameObject, RectTransform) CreateSelectionGameObject()
        {
            var selectionArea = new GameObject("SelectionArea");

            if (canvas == null)
            {
                var canvasObj = new GameObject("SelectionCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 30;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            selectionArea.transform.SetParent(canvas.transform, false);

            var selectionRectTransform = selectionArea.AddComponent<RectTransform>();
            var selectionImage = selectionArea.AddComponent<Image>();
            selectionImage.color = new Color(0f, 1f, 0f, 0.1f);

            return (selectionArea, selectionRectTransform);
        }

        public void UpdateSelectionRect(Vector2 startPos, Vector2 currentPos)
        {
            var center = (startPos + currentPos) / 2;
            var size = new Vector2(
                Mathf.Abs(currentPos.x - startPos.x),
                Mathf.Abs(currentPos.y - startPos.y)
            );

            _selectionAreaPosition.x = center.x;
            _selectionAreaPosition.y = center.y;
            _selectionAreaPosition.z = -29f;

            SelectionRectTransform.position = _selectionAreaPosition;
            SelectionRectTransform.sizeDelta = size;
        }

        private void OnShiftStart()
        {
            IsShiftHold = true;
        }

        private void OnShiftEnd()
        {
            IsShiftHold = false;
        }
    }

    public enum SelectedStates
    {
        NothingSelect,
        UnitSelect,
        BuildingSelect,
        SupplySelect
    }
}
