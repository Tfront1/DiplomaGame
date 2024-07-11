public class TilemapObject
{
	private MapGrid<TilemapObject> _grid;
	private int _x;
	private int _y;
	private TilemapSprite _tilemapSprite;

	public TilemapObject(MapGrid<TilemapObject> grid, int x, int y)
	{
		_grid = grid;
		_x = x;
		_y = y;
	}

	public void SetTilemapSprite(TilemapSprite tilemapSprite)
	{
		_tilemapSprite = tilemapSprite;
		_grid.TriggerGridObjectChanged(_x, _y);
	}

	public TilemapSprite GetTilemapSprite()
	{
		return _tilemapSprite;
	}

	public override string ToString()
	{
		return _tilemapSprite.ToString();
	}
}
