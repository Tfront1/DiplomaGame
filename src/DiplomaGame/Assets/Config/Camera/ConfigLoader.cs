using System.IO;
using UnityEngine;

public static partial class ConfigLoader
{
	public static void LoadCameraConfig()
	{
		var json = File.ReadAllText(ConfigPaths.CameraConfigPath);
		var cameraConfigDto = JsonUtility.FromJson<CameraConfigDto>(json);

		if (cameraConfigDto == null)
		{
			Debug.Log("Error camera config");
			return;
		}

		CameraConfig.EdgeMoveSpeed = cameraConfigDto.EdgeMoveSpeed;
		CameraConfig.EdgeSize = cameraConfigDto.EdgeSize;
		CameraConfig.MiddleMouseSpeed = cameraConfigDto.MiddleMouseSpeed;
		CameraConfig.MinZoom = cameraConfigDto.MinZoom;
		CameraConfig.MaxZoom = cameraConfigDto.MaxZoom;
		CameraConfig.StepZoom = cameraConfigDto.StepZoom;
		CameraConfig.CurrentZoom = cameraConfigDto.CurrentZoom;
		CameraConfig.ZoomSpeed = cameraConfigDto.ZoomSpeed;

		Debug.Log("Camera config loaded");
	}
}
