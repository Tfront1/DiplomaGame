using System;
using UnityEngine;
using GameUtilities.Utils;

public class MapGrid<TGridObject>
{
	public int Width { get; }
	public int Height { get; }
	public float CellSize { get; }
	public Vector3 OriginPosition { get; }

	public readonly TGridObject[,] _gridArray;

	public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;

	//Debug
	private readonly TextMesh[,] _debugTextArray;

	public MapGrid(int width, int height, float cellSize, Vector3 originPosition,
		Func<MapGrid<TGridObject>, int, int, TGridObject> createGridObject)
	{
		Width = width;
		Height = height;
		CellSize = cellSize;
		OriginPosition = originPosition;

		_gridArray = new TGridObject[width, height];
		for (int x = 0; x < _gridArray.GetLength(0); x++)
		{
			for (int y = 0; y < _gridArray.GetLength(1); y++)
			{
				_gridArray[x, y] = createGridObject(this, x, y);
			}
		}

		//Debug code, please enable gizmos in unity to see grid
		bool isDebugMode = true;
		if (isDebugMode)
		{
			_debugTextArray = new TextMesh[width, height];

			for (int x = 0; x < _gridArray.GetLength(0); x++)
			{
				for (int y = 0; y < _gridArray.GetLength(1); y++)
				{
					_debugTextArray[x, y] = UtilsClass.CreateWorldText(_gridArray[x, y]?.ToString(), null,
						GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white,
						TextAnchor.MiddleCenter);

					Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
					Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
				}
				Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
				Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

				OnGridValueChanged += (_, eventArgs) =>
				{
					_debugTextArray[eventArgs.x, eventArgs.y].text = _gridArray[eventArgs.x, eventArgs.y]?.ToString();
				};
			}
		}
	}

	public Vector3 GetWorldPosition(int x, int y)
	{
		return new Vector3(x, y) * CellSize + OriginPosition;
	}

	private Vector2Int GetXY(Vector3 wordPosition)
	{
		return new Vector2Int(
			Mathf.FloorToInt((wordPosition - OriginPosition).x / CellSize),
			Mathf.FloorToInt((wordPosition - OriginPosition).y / CellSize));
	}

	public void TriggerGridObjectChanged(int x, int y)
	{
		OnGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs() { x = x, y = y });
	}

	private void SetGridObject(int x, int y, TGridObject value)
	{
		if (x >= 0 && x < Width && y >= 0 && y < Height)
		{
			_gridArray[x, y] = value;
			OnGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs() { x = x, y = y });

			//Debug
			_debugTextArray[x, y].text = _gridArray[x, y].ToString();
		}
	}

	public void SetGridObject(Vector3 wordPosition, TGridObject value)
	{
		var cellPosition = GetXY(wordPosition);

		SetGridObject(cellPosition.x, cellPosition.y, value);
	}

	public TGridObject GetGridObject(int x, int y)
	{
		if (x >= 0 && x < Width && y >= 0 && y < Height)
		{
			return _gridArray[x, y];
		}
		return default;
	}

	public TGridObject GetGridObject(Vector3 wordPosition)
	{
		var cellPosition = GetXY(wordPosition);

		return GetGridObject(cellPosition.x, cellPosition.y);
	}
}
