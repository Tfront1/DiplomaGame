using GameUtilities.Utils;
using System.Collections.Generic;
using UnityEngine;

public class TilemapVisual : MonoBehaviour
{
	private MapGrid<TilemapObject> _grid;
	private Mesh _mesh;
	private bool _updateMesh;

    public List<TilemapSprite> _allTilemapSprites;

	private void Awake()
    {
        LoadAllVariables();

	}

	public void SetGrid(MapGrid<TilemapObject> grid) 
	{
		_grid = grid;
		UpdateTilemapVisual();

		_grid.OnGridValueChanged += MapGrid_OnGridValueChanged;
	}

	private void MapGrid_OnGridValueChanged(object sender, OnGridValueChangedEventArgs e) 
	{
		_updateMesh = true;
	}

	private void LateUpdate()
	{
		if(_updateMesh) 
		{
			_updateMesh = false;
			UpdateTilemapVisual();
		}
	}

    private void UpdateTilemapVisual()
    {
        MeshUtils.CreateEmptyMeshArrays(_grid.Width * _grid.Height, out var vertices, out var uv, out var triangles);

        for (var x = 0; x < _grid.Width; x++)
        {
            for (var y = 0; y < _grid.Height; y++)
            {
                var index = x * _grid.Height + y;
                var quadSize = new Vector3(1, 1) * _grid.CellSize;

                var gridObject = _grid.GetGridObject(x, y);
                var tilemapSprite = gridObject.GetTilemapSprite();
                Vector2 gridValueUV00;
                Vector2 gridValueUV11;


                if (TilemapData._uvCoordsDictionary.TryGetValue(tilemapSprite._id, out var uvCoords))
                {
                    gridValueUV00 = uvCoords.uv00;
                    gridValueUV11 = uvCoords.uv11;
                }
                else
                {
                    TilemapData._uvCoordsDictionary.TryGetValue(0,
                        out var nullUvCoords);
                    Debug.LogWarning($"UV coordinates not found for texture ID: {tilemapSprite._id}");
                    gridValueUV00 = nullUvCoords.uv00;
                    gridValueUV11 = nullUvCoords.uv11;
                }
                

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _grid.GetWorldPosition(x, y) + quadSize * 0.5f, 0f, quadSize, gridValueUV00, gridValueUV11);
            }
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }


    private void LoadAllVariables()
    {
        TilemapData.InitialiseMesh();
        _mesh = TilemapData._combinedMesh;

        _allTilemapSprites = TilemapData._tilemapSprites;
    }
}
