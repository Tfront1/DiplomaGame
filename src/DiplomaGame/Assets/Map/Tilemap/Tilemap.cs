using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tilemap
{
	private MapGrid<TilemapObject> _grid;

	public Tilemap(int weight, int height, float cellSize, Vector3 originPosition) {
		_grid = new MapGrid<TilemapObject>(weight, height, cellSize, originPosition, (MapGrid<TilemapObject> g, int x, int y) => new TilemapObject(g, x, y));
	}

	public void SetTilemapSprite(Vector3 wordPosition, TilemapSprite tilemapSprite) 
	{
		TilemapObject tilemapObject = _grid.GetGridObject(wordPosition);
		if(tilemapObject != null)
		{
			tilemapObject.SetTilemapSprite(tilemapSprite);
		}
	}

	public void SetTilemapVisual(TilemapVisual tilemapVisual) 
	{
		tilemapVisual.SetGrid(_grid);
	}
}
