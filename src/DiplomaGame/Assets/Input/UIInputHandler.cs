using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputHandler : MonoBehaviour
{
    private static UIInputHandler _instance;

    public static UIInputHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIInputHandler>();
            }
            return _instance;
        }
    }

    private InputActionAsset _inputActions;

    private InputAction _shiftAction;
    public delegate void ShiftClickHandler();
    public event ShiftClickHandler OnShiftStart;
    public event ShiftClickHandler OnShiftEnd;

    private void OnEnable()
    {
        SetupInputAction();

        _shiftAction = _inputActions.FindActionMap("UI").FindAction("Shift");
        _shiftAction.performed += OnShiftPressed;
        _shiftAction.canceled += OnShiftReleased;
    }

    private void OnDisable()
    {
        _shiftAction.performed -= OnShiftPressed;
        _shiftAction.canceled -= OnShiftReleased;
    }

    private void OnShiftPressed(InputAction.CallbackContext context)
    {
        OnShiftStart?.Invoke();
    }

    private void OnShiftReleased(InputAction.CallbackContext context)
    {
        OnShiftEnd?.Invoke();
    }

    private void SetupInputAction()
    {
        _inputActions = InputSystemSetup.Instance.InputActionAsset;
    }
}
