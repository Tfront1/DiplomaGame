using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class InputSystemSetup : MonoBehaviour
{
    public static InputSystemSetup Instance { get; private set; }

    public InputActionAsset InputActionAsset { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeInputActionAsset();
        SetupEventSystem();
        SetupUIInputActions();
    }

    private void InitializeInputActionAsset()
    {
        if (InputActionAsset == null)
        {
            InputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();

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
            var actionMap = InputActionAsset.AddActionMap(actionMapConfig.MapName);

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

            inputModule.actionsAsset = InputActionAsset;
        }
        else
        {

            var inputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.actionsAsset = InputActionAsset;
        }
    }

    private void SetupUIInputActions()
    {
        var inputActions = Instance.InputActionAsset;
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

            var scrollAction = uiMap.AddAction("Scroll", InputActionType.PassThrough);
            scrollAction.AddBinding("<Mouse>/scroll");

            var shiftAction = uiMap.AddAction("Shift", InputActionType.Button);
            shiftAction.AddBinding("<Keyboard>/leftShift");
            shiftAction.AddBinding("<Keyboard>/rightShift");

            uiMap.Enable();
        }

        var inputModule = EventSystem.current?.GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            inputModule.point = InputActionReference.Create(uiMap.FindAction("Point"));
            inputModule.leftClick = InputActionReference.Create(uiMap.FindAction("LeftClick"));
            inputModule.rightClick = InputActionReference.Create(uiMap.FindAction("RightClick"));

            inputModule.scrollWheel = InputActionReference.Create(uiMap.FindAction("Scroll"));
        }
    }
}
