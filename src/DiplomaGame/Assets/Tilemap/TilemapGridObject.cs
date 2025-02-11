using System;

public class TilemapGridObject : IGridObject
{

    private MapGrid<TilemapGridObject> _grid;
	private int _x;
	private int _y;
    public TilemapSprite _tilemapSprite;

    public int X => _x;
    public int Y => _y;
    public Guid Guid => default;

    public TilemapGridObject(MapGrid<TilemapGridObject> grid, int x, int y, TilemapSprite tilemapSprite)
	{
		_grid = grid;
		_x = x;
		_y = y;
        _tilemapSprite = tilemapSprite;

    }

	public void SetTilemapSprite(TilemapSprite tilemapSprite)
    {
        _tilemapSprite = tilemapSprite;
        _grid.TriggerGridObjectChanged(_x, _y);
	}

    public Guid GetGuid()
    {
        return default;
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
