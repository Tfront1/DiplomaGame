using System;

public class BuildingGridObject
{
	private int _x;
	private int _y;
	private Guid _guid;

	public BuildingGridObject(MapGrid<BuildingGridObject> grid, int x, int y, Guid guid = default)
	{
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