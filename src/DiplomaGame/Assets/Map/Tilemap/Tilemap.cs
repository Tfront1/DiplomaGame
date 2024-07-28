using UnityEngine;

public class Tilemap
{
	private MapGrid<TilemapObject> _grid;

	public Tilemap(int weight, int height, float cellSize, Vector3 originPosition) {
		_grid = new MapGrid<TilemapObject>(weight, height, cellSize, originPosition, (MapGrid<TilemapObject> g, int x, int y) => new TilemapObject(g, x, y, new TilemapSprite(TerrainTexturesConfig._terrainTextures[0].id, TerrainTexturesConfig._terrainTextures[0].name)));
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
