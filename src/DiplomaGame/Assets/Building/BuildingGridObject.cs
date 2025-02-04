using System;

public class BuildingGridObject
{
	private MapGrid<BuildingGridObject> _grid;
	private int _x;
	private int _y;
	private Guid _guid;

	public BuildingGridObject(MapGrid<BuildingGridObject> grid, int x, int y, Guid guid = default)
	{
		_grid = grid;
		_x = x;
		_y = y;
		_guid = guid;
    }

    public override string ToString()
	{
		return _x + ", " + _y;
	}

    public Guid GetGuid()
    {
        return _guid;
    }
}