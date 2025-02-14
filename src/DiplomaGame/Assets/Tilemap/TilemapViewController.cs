using System.Threading.Tasks;
using GameUtilities.MonoBehaviours;
using UnityEngine;

public class TilemapViewController
{
    private Tilemap _tilemap;
    private int[,] _map;
    private TilemapDisplay _tilemapDisplay = new();

    public void Init(int[,] map, Tilemap tilemap)
    {
        _map = map;
        _tilemap = tilemap;
        CameraManager.SubscribeToCameraMove(OnCameraMoved);
        _tilemapDisplay.DisplayVisibleChunks(_map, _tilemap, Camera.main);
    }
    
    public void Stop()
    {
        CameraManager.UnsubscribeFromCameraMove(OnCameraMoved);
    }

    private void OnCameraMoved(CameraMoveEventArgs args)
    {
        _tilemapDisplay.DisplayVisibleChunks(
            _map,
            _tilemap,
            args.BottomLeft,
            args.TopRight
        );
    }
}
