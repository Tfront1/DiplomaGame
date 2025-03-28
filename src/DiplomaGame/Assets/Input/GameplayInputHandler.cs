using System;
using System.Collections.Generic;
using GameUtilities.MonoBehaviours;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameplayInputHandler : MonoBehaviour
{
    private static GameplayInputHandler _instance;

    public static GameplayInputHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameplayInputHandler>();
            }
            return _instance;
        }
    }

    private InputActionAsset _inputActions;

    private bool _isPointerOverUI = false;
    private bool _mousePositionChanged = false;
    private Canvas _specificCanvas;
    private GraphicRaycaster _graphicRaycaster;
    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;

    // Mouse position
    public delegate void MousePositionHandler(Vector2 position);
    public event MousePositionHandler OnMousePosition;

    // Middle mouse
    public delegate void MiddleMouseHoldHandler(bool flag);
    public event MiddleMouseHoldHandler OnMiddleMouseHold;

    private InputAction _holdMiddleClickAction;
    private bool _isHoldingMiddleClick;

    // Scroll mouse
    public delegate void ScrollMouseHandler(int scrollValue);
    public event ScrollMouseHandler OnScroll;
    
    private InputAction _scrollWheelAction;

    // Mouse edge
    public delegate void MouseEdgeHandler(Vector2 position);
    public event MouseEdgeHandler OnMouseNearEdge;

    private InputAction _mousePositionAction;
    private Vector2 _mouseScreenPosition;
    private Vector2 _mouseWorldPosition;
    private float _positionUpdateTimer = 0f;
    private const float _positionUpdateInterval = 0.00f;

    // Left mouse click
    public delegate void MouseLeftClickHandler(Vector2 position);
    public event MouseLeftClickHandler OnMouseLeftClick;
    public event MouseLeftClickHandler OnMouseLeftUp;

    private float _clickThreshold = 0.1f;
    private float _leftMousePressTime;

    // Left mouse hold
    public delegate void MouseLeftHoldHandler(Vector2 position);
    public event MouseLeftHoldHandler OnMouseLeftHoldStart;
    public event MouseLeftHoldHandler OnMouseLeftHold;
    public event MouseLeftHoldHandler OnMouseLeftHoldEnd;

    private bool _holdEventTriggered = false;
    public bool _isMouseLeftHold = false;
    private InputAction _mouseLeftHoldAction;

    // Right mouse click
    public delegate void MouseRightClickHandler(Vector2 position);
    public event MouseRightClickHandler OnMouseRightClick;
    public event MouseRightClickHandler OnMouseRightUp;

    private InputAction _mouseRightClickAction;

    // Right mouse hold
    public delegate void MouseRightHoldHandler(Vector2 position);
    public event MouseRightHoldHandler OnMouseRightHoldStart;
    public event MouseRightHoldHandler OnMouseRightHold;
    public event MouseRightHoldHandler OnMouseRightHoldEnd;

    public bool _isMouseRightHold = false;
    private InputAction _mouseRightHoldAction;

    private void Awake()
    {
        _specificCanvas = MainCanvasUI.MainCanvas;
        _graphicRaycaster = _specificCanvas.GetComponent<GraphicRaycaster>();
        _eventSystem = EventSystem.current;
        _pointerEventData = new PointerEventData(_eventSystem);
    }

    private void OnEnable()
    {
        SetupInputAction();
        
        var playerActions = _inputActions.FindActionMap("Gameplay") ?? throw new Exception();

        _holdMiddleClickAction = playerActions.FindAction("CameraMiddleMouseMovement") ?? throw new Exception();
        _scrollWheelAction = playerActions.FindAction("MouseScroll") ?? throw new Exception();
        _mousePositionAction = playerActions.FindAction("MousePosition") ?? throw new Exception();
        _mouseLeftHoldAction = playerActions.FindAction("MouseLeftHold") ?? throw new Exception();
        _mouseRightClickAction = playerActions.FindAction("MouseRightClick") ?? throw new Exception();
        _mouseRightHoldAction = playerActions.FindAction("MouseRightHold") ?? throw new Exception();

        _holdMiddleClickAction.performed += OnHoldMiddleClick;
        _holdMiddleClickAction.canceled += OnReleaseMiddleClick;
        _scrollWheelAction.performed += OnScrollMouse;
        _mousePositionAction.performed += OnMousePositionNearEdge;
        _mousePositionAction.performed += MousePosition;

        _mouseLeftHoldAction.started += MouseLeftHoldStart;
        _mouseLeftHoldAction.canceled += MouseLeftHoldEnd;
        _mousePositionAction.performed += MouseLeftHold;

        _mouseRightClickAction.performed += MouseRightDown;
        _mouseRightClickAction.canceled += MouseRightUp;
        _mouseRightHoldAction.started += MouseRightHoldStart;
        _mouseRightHoldAction.canceled += MouseRightHoldEnd;
        _mousePositionAction.performed += MouseRightHold;

        _holdMiddleClickAction.Enable();
        _scrollWheelAction.Enable();
        _mousePositionAction.Enable();
        _mouseLeftHoldAction.Enable();
        _mouseRightClickAction.Enable();
        _mouseRightHoldAction.Enable();
    }

    private void OnDisable()
    {
        _holdMiddleClickAction.performed -= OnHoldMiddleClick;
        _holdMiddleClickAction.canceled -= OnReleaseMiddleClick;

        _scrollWheelAction.performed -= OnScrollMouse;

        _mousePositionAction.performed -= OnMousePositionNearEdge;
        _mousePositionAction.performed -= MousePosition;

        _mouseLeftHoldAction.started -= MouseLeftHoldStart;
        _mouseLeftHoldAction.canceled -= MouseLeftHoldEnd;
        _mousePositionAction.performed -= MouseLeftHold;

        _mouseRightClickAction.performed -= MouseRightDown;
        _mouseRightClickAction.canceled -= MouseRightUp;
        _mouseRightHoldAction.started -= MouseRightHoldStart;
        _mouseRightHoldAction.canceled -= MouseRightHoldEnd;
        _mousePositionAction.performed -= MouseRightHold;

        _holdMiddleClickAction.Disable();
        _scrollWheelAction.Disable();
        _mousePositionAction.Disable();
        _mouseLeftHoldAction.Disable();
        _mouseRightClickAction.Disable();
        _mouseRightHoldAction.Disable();
    }

    private void OnHoldMiddleClick(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        _isHoldingMiddleClick = true;
        OnMiddleMouseHold?.Invoke(_isHoldingMiddleClick);
    }

    private void OnReleaseMiddleClick(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        _isHoldingMiddleClick = false;
        OnMiddleMouseHold?.Invoke(_isHoldingMiddleClick);
    }

    private void OnScrollMouse(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        var scrollValue = context.ReadValue<Vector2>();
        var scrollDirection = scrollValue.y > 0 ? 1 : -1;
        OnScroll?.Invoke(scrollDirection);
    }

    private void OnMousePositionNearEdge(InputAction.CallbackContext context)
    {
        _positionUpdateTimer += Time.deltaTime;
        if (_positionUpdateTimer >= _positionUpdateInterval)
        {
            _mouseScreenPosition = context.ReadValue<Vector2>();
            _mouseWorldPosition = CameraFollow.GetWorldPosition(_mouseScreenPosition);
            _positionUpdateTimer = 0f;
        }
    }

    private void MousePosition(InputAction.CallbackContext context)
    {
        _mousePositionChanged = true;
        OnMousePosition?.Invoke(_mouseWorldPosition);
    }

#region RightMouseButton

    private void MouseRightDown(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        OnMouseRightClick?.Invoke(_mouseWorldPosition);
    }

    private void MouseRightUp(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        OnMouseRightUp?.Invoke(_mouseWorldPosition);
    }

    private void MouseRightHoldStart(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        OnMouseRightHoldStart?.Invoke(_mouseWorldPosition);
        _isMouseLeftHold = true;
    }

    private void MouseRightHold(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        if (_isMouseRightHold)
        {
            OnMouseRightHold?.Invoke(_mouseWorldPosition);
        }
    }

    private void MouseRightHoldEnd(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        OnMouseRightHoldEnd?.Invoke(_mouseWorldPosition);
        _isMouseRightHold = false;
    }

    #endregion RightMouseButton

    #region LeftMouseButton
    private void MouseLeftHoldStart(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        _leftMousePressTime = Time.time;
        _isMouseLeftHold = true;
    }

    private void MouseLeftHold(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;
        if (_isMouseLeftHold)
        {
            if (Time.time - _leftMousePressTime >= _clickThreshold)
            {
                if (!_holdEventTriggered)
                {
                    OnMouseLeftHoldStart?.Invoke(_mouseWorldPosition);
                    _holdEventTriggered = true;
                }

                OnMouseLeftHold?.Invoke(_mouseWorldPosition);
            }
        }
    }

    private void MouseLeftHoldEnd(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI)
            return;

        var holdDuration = Time.time - _leftMousePressTime;

        if (holdDuration < _clickThreshold)
        {
            OnMouseLeftClick?.Invoke(_mouseWorldPosition);
        }
        else if (_holdEventTriggered)
        {
            OnMouseLeftHoldEnd?.Invoke(_mouseWorldPosition);
        }

        _isMouseLeftHold = false;
        _holdEventTriggered = false;

        OnMouseLeftUp?.Invoke(_mouseWorldPosition);
    }

    #endregion

    private void Update()
    {
        if (_isHoldingMiddleClick)
        {
            OnMiddleMouseHold?.Invoke(_isHoldingMiddleClick);
        }
        else
        {
            OnMouseNearEdge?.Invoke(_mouseScreenPosition);
        }

        if (_mousePositionChanged)
        {
            _isPointerOverUI = IsPointerOverSpecificCanvas();
            _mousePositionChanged = false;
        }
    }

    private bool IsPointerOverSpecificCanvas()
    {
        if (_graphicRaycaster == null || _eventSystem == null)
            return false;

        _pointerEventData.position = Mouse.current.position.ReadValue();

        var results = new List<RaycastResult>();

        _graphicRaycaster.Raycast(_pointerEventData, results);

        return results.Count > 0;
    }

    private void SetupInputAction()
    {
        _inputActions = InputSystemSetup.Instance.InputActionAsset;
    }
}