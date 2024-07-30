using GameUtilities.Utils;
using UnityEngine;

public class TilemapVisual : MonoBehaviour
{
	private MapGrid<TilemapObject> _grid;
	private bool _updateMesh;

    public TilemapChunkManager _chunkManager;

    private int _x, _y;

	private void Awake()
    {
        LoadTilemapChunks();

	}

	public void SetGrid(MapGrid<TilemapObject> grid) 
	{
		_grid = grid;
		UpdateTilemapVisual(_x, _y);

		_grid.OnGridValueChanged += MapGrid_OnGridValueChanged;
	}

    /// <summary>
    /// Handles changes in the grid value, triggering a mesh update.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments containing the x and y coordinates of the changed grid value.</param>
    /// <remarks>
    /// This method sets a flag to indicate that the mesh needs to be updated and stores the coordinates 
    /// of the changed grid value. It is intended to respond to updates in the grid data and refresh the mesh accordingly.
    /// </remarks>
    private void MapGrid_OnGridValueChanged(object sender, OnGridValueChangedEventArgs e) 
	{
		_updateMesh = true;
        _x = e.x;
        _y = e.y;
    }

	private void LateUpdate()
	{
		if(_updateMesh) 
		{
			_updateMesh = false;
			UpdateTilemapVisual(_x, _y);
		}
	}

    /// <summary>
    /// Updates the tilemap visual at the specified grid coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate of the grid point.</param>
    /// <param name="y">The y-coordinate of the grid point.</param>
    /// <remarks>
    /// This method updates the mesh for a specific tile in the tilemap. It calculates the necessary UV coordinates,
    /// updates the mesh arrays with new vertices, UVs, and triangles, and then applies the updated mesh to the `combinedMesh`.
    /// </remarks>
    private void UpdateTilemapVisual(int x, int y)
    {
        var gridPointCoords = new Vector3(x, y);
        var chunkPointCoords = _chunkManager.GetLocalPositionInChunk(gridPointCoords);

        var tilemapData = _chunkManager.GetTilemapDataAtChunkPoint(gridPointCoords);
        
        var index = _chunkManager.GetGridPositionChunkIndex(gridPointCoords);
        var quadSize = new Vector3(1, 1) * _grid.CellSize;

        var gridObject = _grid.GetGridObject(x, y);
        var tilemapSprite = gridObject.GetTilemapSprite();
        Vector2 gridValueUV00;
        Vector2 gridValueUV11;
        
        if (tilemapData._uvCoordsDictionary.TryGetValue(tilemapSprite._id, out var uvCoords))
        {
            gridValueUV00 = uvCoords.uv00;
            gridValueUV11 = uvCoords.uv11;
        }
        else
        {
            tilemapData._uvCoordsDictionary.TryGetValue(0,
                out var nullUvCoords);
            Debug.LogWarning($"UV coordinates not found for texture ID: {tilemapSprite._id}");
            gridValueUV00 = nullUvCoords.uv00;
            gridValueUV11 = nullUvCoords.uv11;
        }
        
        MeshUtils.AddToMeshArrays(tilemapData._vertices, tilemapData._uv, tilemapData._triangles, index, chunkPointCoords * _grid.CellSize + quadSize * 0.5f, 0f, quadSize,  gridValueUV00, gridValueUV11);

        tilemapData._combinedMesh.vertices = tilemapData._vertices;
        tilemapData._combinedMesh.uv = tilemapData._uv;
        tilemapData._combinedMesh.triangles = tilemapData._triangles;
        
        //Recalculate bounds for property rendering. Must have
        tilemapData._combinedMesh.RecalculateBounds();
    }


    private void LoadTilemapChunks()
    {
        _chunkManager = TilemapChunkManager.Instance;
    }
}
