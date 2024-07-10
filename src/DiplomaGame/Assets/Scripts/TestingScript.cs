using GameUtilities.Utils;
using UnityEngine;

public class TestingScript : MonoBehaviour
{
	private MapGrid<int> _grid;

	private void Start()
	{
		_grid = new MapGrid<int>(30, 30, 10f, new Vector3(-150, -150), (MapGrid<int> g, int x, int y) => new int());
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			_grid.SetGridObject(UtilsClass.GetMouseWorldPosition(), 56);
		}

		if (Input.GetMouseButtonDown(1))
		{
			Debug.Log(_grid.GetGridObject(UtilsClass.GetMouseWorldPosition()));
		}
	}
}