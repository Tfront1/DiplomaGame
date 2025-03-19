using System;
using GameUtilities.MonoBehaviours;
using UnityEngine;
using UnityEngine.InputSystem;

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

                if (_instance == null)
                {
                    var managerObject = new GameObject("GameplayInputHandler");
                    _instance = managerObject.AddComponent<GameplayInputHandler>();
                }
            }
            return _instance;
        }
    }

    private InputActionAsset _inputActions;

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
    private const float _positionUpdateInterval = 0f;

    // Left mouse click
    public delegate void MouseLeftClickHandler(Vector2 position);
    public event MouseLeftClickHandler OnMouseLeftClick;
    public event MouseLeftClickHandler OnMouseLeftUp;

    private InputAction _mouseLeftClickAction;

    // Left mouse hold
    public delegate void MouseLeftHoldHandler(Vector2 position);
    public event MouseLeftHoldHandler OnMouseLeftHoldStart;
    public event MouseLeftHoldHandler OnMouseLeftHold;
    public event MouseLeftHoldHandler OnMouseLeftHoldEnd;

    public bool _isMouseLeftHold = false;

    private InputAction _mouseLeftHoldAction;

    private void OnEnable()
    {
        SetupInputAction();
        
        var playerActions = _inputActions.FindActionMap("Gameplay") ?? throw new Exception();

        _holdMiddleClickAction = playerActions.FindAction("CameraMiddleMouseMovement") ?? throw new Exception();
        _scrollWheelAction = playerActions.FindAction("MouseScroll") ?? throw new Exception();
        _mousePositionAction = playerActions.FindAction("MousePosition") ?? throw new Exception();
        _mouseLeftClickAction = playerActions.FindAction("MouseLeftClick") ?? throw new Exception();
        _mouseLeftHoldAction = playerActions.FindAction("MouseLeftHold") ?? throw new Exception();

        _holdMiddleClickAction.performed += OnHoldMiddleClick;
        _holdMiddleClickAction.canceled += OnReleaseMiddleClick;
        _scrollWheelAction.performed += OnScrollMouse;
        _mousePositionAction.performed += OnMousePosition;

        _mouseLeftClickAction.performed += MouseLeftDown;
        _mouseLeftClickAction.canceled += MouseLeftUp;
        _mouseLeftHoldAction.started += MouseLeftHoldStart;
        _mouseLeftHoldAction.canceled += MouseLeftHoldEnd;
        _mousePositionAction.performed += MouseLeftHold;


        _holdMiddleClickAction.Enable();
        _scrollWheelAction.Enable();
        _mousePositionAction.Enable();
        _mouseLeftClickAction.Enable();
        _mouseLeftHoldAction.Enable();
    }

    private void OnDisable()
    {
        _holdMiddleClickAction.performed -= OnHoldMiddleClick;
        _holdMiddleClickAction.canceled -= OnReleaseMiddleClick;

        _scrollWheelAction.performed -= OnScrollMouse;

        _mousePositionAction.performed -= OnMousePosition;

        _mouseLeftClickAction.performed -= MouseLeftDown;
        _mouseLeftClickAction.canceled -= MouseLeftUp;
        _mouseLeftHoldAction.started -= MouseLeftHoldStart;
        _mouseLeftHoldAction.canceled -= MouseLeftHoldEnd;
        _mousePositionAction.performed -= MouseLeftHold;

        _holdMiddleClickAction.Disable();
        _scrollWheelAction.Disable();
        _mousePositionAction.Disable();
        _mouseLeftClickAction.Disable();
        _mouseLeftHoldAction.Disable();
    }

    private void OnHoldMiddleClick(InputAction.CallbackContext context)
    {
        _isHoldingMiddleClick = true;
        OnMiddleMouseHold?.Invoke(_isHoldingMiddleClick);
    }

    private void OnReleaseMiddleClick(InputAction.CallbackContext context)
    {
        _isHoldingMiddleClick = false;
        OnMiddleMouseHold?.Invoke(_isHoldingMiddleClick);
    }

    private void OnScrollMouse(InputAction.CallbackContext context)
    {
        var scrollValue = context.ReadValue<Vector2>();
        var scrollDirection = scrollValue.y > 0 ? 1 : -1;
        OnScroll?.Invoke(scrollDirection);
    }

    private void OnMousePosition(InputAction.CallbackContext context)
    {
        _positionUpdateTimer += Time.deltaTime;
        if (_positionUpdateTimer >= _positionUpdateInterval)
        {
            _mouseScreenPosition = context.ReadValue<Vector2>();
            _mouseWorldPosition = CameraFollow.GetWorldPosition(_mouseScreenPosition);
            _positionUpdateTimer = 0f;
        }
    }

    private void MouseLeftDown(InputAction.CallbackContext context)
    {
        OnMouseLeftClick?.Invoke(_mouseWorldPosition);
    }

    private void MouseLeftUp(InputAction.CallbackContext context)
    {
        OnMouseLeftUp?.Invoke(_mouseWorldPosition);
    }

    private void MouseLeftHoldStart(InputAction.CallbackContext context)
    {
        OnMouseLeftHoldStart?.Invoke(_mouseWorldPosition);
        _isMouseLeftHold = true;
    }

    private void MouseLeftHold(InputAction.CallbackContext context)
    {
        if (_isMouseLeftHold)
        {
            OnMouseLeftHold?.Invoke(_mouseWorldPosition);
        }
    }

    private void MouseLeftHoldEnd(InputAction.CallbackContext context)
    {
        OnMouseLeftHoldEnd?.Invoke(_mouseWorldPosition);
        _isMouseLeftHold = false;
    }

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
    }

    private void SetupInputAction()
    {
        _inputActions = InputSystemSetup._instance._inputActionAsset;
    }
}