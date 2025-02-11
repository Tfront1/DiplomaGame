using UnityEngine;

public class Tilemap
{
	private MapGrid<TilemapGridObject> _grid;

	public Tilemap(int weight, int height, float cellSize, Vector3 originPosition) {
		_grid = new MapGrid<TilemapGridObject>(weight, height, cellSize, originPosition, (g, x, y) => new TilemapGridObject(g, x, y, new TilemapSprite(TerrainTexturesConfig.TerrainTextures[0].Id)));
	}

	public void SetTilemapSprite(Vector3 wordPosition, TilemapSprite tilemapSprite) 
	{
		var tilemapObject = _grid.GetGridObject(wordPosition);
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
