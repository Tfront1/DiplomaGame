using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIElementContext : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string _contextText = "Data";
    private bool _isPointerOver = false;
    private InputAction _rightClickAction;

    public void SetContextText(string text)
    {
        _contextText = text;
    }
    
    private void Awake()
    {
        var inputActions = InputSystemSetup.Instance.InputActionAsset;
        var uiMap = inputActions.FindActionMap("UI");

        _rightClickAction = uiMap.FindAction("RightClick");
        _rightClickAction.performed += OnRightClick;
    }

    private void OnDestroy()
    {
        if (_rightClickAction != null)
        {
            _rightClickAction.performed -= OnRightClick;
        }
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (_isPointerOver)
        {
            ContextPanel.Instance.ShowContext(_contextText);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        ContextPanel.Instance.HideContext();
    }
}