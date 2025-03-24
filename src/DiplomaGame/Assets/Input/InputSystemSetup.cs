using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class InputSystemSetup : MonoBehaviour
{
    public static InputSystemSetup _instance { get; private set; }

    public InputActionAsset _inputActionAsset { get; private set; }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeInputActionAsset();
        SetupEventSystem();
        SetupUIInputActions();
    }

    private void InitializeInputActionAsset()
    {
        if (_inputActionAsset == null)
        {
            _inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();

            SetupInputActionAsset();
        }
        else
        {
            Debug.LogWarning("InputActionAsset already created.");
        }
    }

    private void SetupInputActionAsset()
    {
        foreach (var actionMapConfig in InputSystemConfig.ActionMap)
        {
            var actionMap = _inputActionAsset.AddActionMap(actionMapConfig.MapName);

            foreach (var actionConfig in actionMapConfig.Actions)
            {
                var actionType = (InputActionType)System.Enum.Parse(typeof(InputActionType), actionConfig.ActionType);
                var interactions = !string.IsNullOrEmpty(actionConfig.Interactions) ? actionConfig.Interactions : null;

                var action = actionMap.AddAction(name: actionConfig.ActionName,
                    type: actionType,
                    interactions: interactions);

                if (!string.IsNullOrEmpty(actionConfig.ExpectedControlType))
                {
                    action.expectedControlType = actionConfig.ExpectedControlType;
                }

                foreach (var bindingConfig in actionConfig.ActionBindings)
                {
                    action.AddBinding(bindingConfig.BindingPath)
                        .WithGroup(bindingConfig.BindingGroup);
                }
            }
        }
    }

    private void SetupEventSystem()
    {
        if (EventSystem.current == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            var eventSystem = eventSystemObj.AddComponent<EventSystem>();

            var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();

            inputModule.actionsAsset = _inputActionAsset;
        }
        else
        {

            var inputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.actionsAsset = _inputActionAsset;
        }
    }

    private void SetupUIInputActions()
    {
        var inputActions = _instance._inputActionAsset;

        var uiMap = inputActions.FindActionMap("UI");
        if (uiMap == null)
        {
            uiMap = inputActions.AddActionMap("UI");

            var pointAction = uiMap.AddAction("Point", InputActionType.PassThrough);
            pointAction.AddBinding("<Mouse>/position");
            pointAction.AddBinding("<Pen>/position");
            pointAction.AddBinding("<Touchscreen>/touch*/position");

            var clickAction = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            clickAction.AddBinding("<Mouse>/leftButton");
            clickAction.AddBinding("<Pen>/tip");
            clickAction.AddBinding("<Touchscreen>/touch*/press");

            var rightClickAction = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClickAction.AddBinding("<Mouse>/rightButton");

            uiMap.Enable();

        }

        var inputModule = EventSystem.current?.GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            inputModule.point = InputActionReference.Create(uiMap.FindAction("Point"));
            inputModule.leftClick = InputActionReference.Create(uiMap.FindAction("LeftClick"));
            inputModule.rightClick = InputActionReference.Create(uiMap.FindAction("RightClick"));
        }
    }
}