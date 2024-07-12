using GameUtilities.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TilemapVisual : MonoBehaviour
{
	[Serializable]
	public struct TilemapSpriteUV 
	{
		public TilemapSprite tilemapSprite;
		public Vector2Int uv00Pixels;
		public Vector2Int uv11Pixels;
	}

	private struct UVCoords 
	{
		public Vector2 uv00;
		public Vector2 uv11;
	}

	[SerializeField]
	private TilemapSpriteUV[] _tilemapSpriteUVArray;

	private MapGrid<TilemapObject> _grid;
	private Mesh _mesh;
	private bool _updateMesh;

	private Dictionary<TilemapSprite, UVCoords> _uvCoordsDictionary;

	private void Awake()
	{
		_mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = _mesh;

		Texture texture = GetComponent<MeshRenderer>().material.mainTexture;
		_uvCoordsDictionary = new Dictionary<TilemapSprite, UVCoords>();
		foreach (TilemapSpriteUV tilemapSpriteUV in _tilemapSpriteUVArray) 
		{
			_uvCoordsDictionary[tilemapSpriteUV.tilemapSprite] = new UVCoords
			{
				uv00 = new Vector2(tilemapSpriteUV.uv00Pixels.x / (float)texture.width, tilemapSpriteUV.uv00Pixels.y / (float)texture.height),
				uv11 = new Vector2(tilemapSpriteUV.uv11Pixels.x / (float)texture.width, tilemapSpriteUV.uv11Pixels.y / (float)texture.height),
			};
		}
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
		MeshUtils.CreateEmptyMeshArrays(_grid.Width * _grid.Height, out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

		for(int x = 0; x < _grid.Width; x++) 
		{
			for (int y = 0; y < _grid.Height; y++) 
			{
				int index = x * _grid.Height + y;
				Vector3 quadSize = new Vector3(1,1) * _grid.CellSize;

				TilemapObject gridObject = _grid.GetGridObject(x, y);
				var tilemapSprite = gridObject.GetTilemapSprite();
				Vector2 gridValueUV00 = new(), gridValueUV11 = new();

				if (tilemapSprite == TilemapSprite.None)
				{
					quadSize = Vector2.zero;
				}
				else 
				{
					UVCoords uvCoords = _uvCoordsDictionary[tilemapSprite];
					gridValueUV00 = uvCoords.uv00;
					gridValueUV11 = uvCoords.uv11;
				}
				MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _grid.GetWorldPosition(x, y) + quadSize * 0.5f, 0f, quadSize, gridValueUV00, gridValueUV11);
			}
		}

		_mesh.vertices = vertices;
		_mesh.uv = uv;
		_mesh.triangles = triangles;
	}
}
