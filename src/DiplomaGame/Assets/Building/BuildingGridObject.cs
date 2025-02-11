using System;

public class BuildingGridObject : IGridObject
{
	private int _x;
	private int _y;
	private Guid _guid;

	public int X => _x;
    public int Y => _y;
    public Guid Guid => _guid;

    public BuildingGridObject(MapGrid<BuildingGridObject> grid, int x, int y, Guid guid = default)
	{
		_x = x;
		_y = y;
		_guid = guid;
    }

    public Guid GetGuid()
    {
        return _guid;
    }

    public override string ToString()
	{
		return _x + ", " + _y;
	}
}