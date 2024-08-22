using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayInputHandler : MonoBehaviour
{
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

    // Mouse Edge
    public delegate void MouseEdgeHandler(Vector2 position);
    public event MouseEdgeHandler OnMouseNearEdge;

    private InputAction _mousePositionAction;
    private Vector2 _mousePosition;

    private void OnEnable()
    {
        SetupInputAction();

        var playerActions = _inputActions.FindActionMap("Gameplay") ?? throw new Exception();

        _holdMiddleClickAction = playerActions.FindAction("CameraMiddleMouseMovement") ?? throw new Exception();
        _scrollWheelAction = playerActions.FindAction("MouseScroll") ?? throw new Exception();
        _mousePositionAction = playerActions.FindAction("MousePosition") ?? throw new Exception();

        _holdMiddleClickAction.performed += OnHoldMiddleClick;
        _holdMiddleClickAction.canceled += OnReleaseMiddleClick;
        _scrollWheelAction.performed += OnScrollMouse;
        _mousePositionAction.performed += OnMousePosition;


        _holdMiddleClickAction.Enable();
        _scrollWheelAction.Enable();
        _mousePositionAction.Enable();
    }

    private void OnDisable()
    {
        _holdMiddleClickAction.performed -= OnHoldMiddleClick;
        _holdMiddleClickAction.canceled -= OnReleaseMiddleClick;

        _scrollWheelAction.performed -= OnScrollMouse;

        _mousePositionAction.performed -= OnMousePosition;

        _holdMiddleClickAction.Disable();
        _scrollWheelAction.Disable();
        _mousePositionAction.Disable();
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
        _mousePosition = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        if (_isHoldingMiddleClick)
        {
            OnMiddleMouseHold?.Invoke(_isHoldingMiddleClick);
        }
        else
        {
            OnMouseNearEdge?.Invoke(_mousePosition);
        }
    }

    private void SetupInputAction()
    {
        _inputActions = InputSystemSetup._instance._inputActionAsset;
    }
}