using UnityEngine;
using UnityEngine.InputSystem;

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
}