using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadInputSystemConfig()
	{
		var json = File.ReadAllText(ConfigPaths.InputActionConfigPath);
		var inputSystemDto = JsonUtility.FromJson<InputSystemDto>(json);

		if (inputSystemDto == null)
		{
			Debug.Log("Error input system config");
			return;
		}

		InputSystemConfig.ActionMap = new List<InputSystemConfig.ActionMapConfig>();

		foreach (var map in inputSystemDto.Maps)
		{
			var actionMapConfig = new InputSystemConfig.ActionMapConfig
			{
				MapName = map.name,
				Actions = new List<InputSystemConfig.ActionsConfig>()
			};

			foreach (var action in map.actions)
			{
				var actionsConfig = new InputSystemConfig.ActionsConfig
				{
					ActionName = action.name,
					ActionType = action.type,
					ExpectedControlType = action.expectedControlType,
					ActionBindings = new List<InputSystemConfig.ActionBindingsConfig>()
				};

				foreach (var binding in action.bindings)
				{
					var actionBindingsConfig = new InputSystemConfig.ActionBindingsConfig
					{
						BindingPath = binding.path,
						BindingGroup = binding.group
					};

					actionsConfig.ActionBindings.Add(actionBindingsConfig);
				}

				actionMapConfig.Actions.Add(actionsConfig);
			}

			InputSystemConfig.ActionMap.Add(actionMapConfig);
		}

		Debug.Log("Input System config loaded");

	}
}
