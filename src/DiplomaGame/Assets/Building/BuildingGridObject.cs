public class BuildingGridObject
{
	private MapGrid<BuildingGridObject> _grid;
	private int _x;
	private int _y;

	public BuildingGridObject(MapGrid<BuildingGridObject> grid, int x, int y)
	{
		_grid = grid;
		_x = x;
		_y = y;
	}

	public override string ToString()
	{
		return _x + ", " + _y;
	}
}