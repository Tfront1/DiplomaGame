using UnityEngine;
using UnityEngine.UI;

public class MainCanvasUI
{
    private static Canvas _mainCanvas = null;

    public static Canvas MainCanvas
    {
        get
        {
            if (_mainCanvas == null)
            {
                InitializeCanvas();
            }
            return _mainCanvas;
        }
        private set => _mainCanvas = value;
    }

    private static void InitializeCanvas()
    {
        var canvasObj = new GameObject("MainCanvas");
        MainCanvas = canvasObj.AddComponent<Canvas>();
        MainCanvas.renderMode = RenderMode.ScreenSpaceCamera;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }
}
