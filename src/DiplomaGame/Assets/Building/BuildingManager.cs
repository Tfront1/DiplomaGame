using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingManager : MonoBehaviour
{
	[SerializeField]
	public Texture2D texture;

	private MapGrid<BuildingGridObject> _grid;
	private Building selectedBuilding; // The building selected from the configuration

	private void Awake()
	{
		_grid = new MapGrid<BuildingGridObject>(
			MapConfig.MapWidth,
			MapConfig.MapHeight,
			MapConfig.CellSize,
			new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY),
			(g, x, y) => new BuildingGridObject(g, x, y)
		);

		// Example: Select the first building from the configuration list (you can change this logic)
		selectedBuilding = BuildingsConfig.Buildings.First();
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			// Get the mouse click position
			Vector3 clickPosition = Input.mousePosition;
			clickPosition.z = 0.0f; // Distance from the camera to the object
			Vector3 worldPosition = Camera.main.ScreenToWorldPoint(clickPosition);

			// Create a new game object for the building
			GameObject newBuildingObject = new GameObject(selectedBuilding.Name);

			// Add a SpriteRenderer component to the new object
			SpriteRenderer renderer = newBuildingObject.AddComponent<SpriteRenderer>();

			// Create a sprite from the texture
			Sprite newBuildingSprite = Sprite.Create(
				texture,
				new Rect(0.0f, 0.0f, texture.width, texture.height),
				Vector2.zero
			);
			renderer.sprite = newBuildingSprite;

			// Calculate the position based on the grid
			var gridPosition = _grid.GetCellGridPosition(worldPosition);
			var worldPositionAdjusted = _grid.GetWorldPosition(gridPosition.x, gridPosition.y);
			newBuildingObject.transform.position = worldPositionAdjusted;

			// Розмір текстури в ігрових одиницях (фактичний розмір текстури на сцені)
			float textureUnitWidth = texture.width / 100.0f;
			float textureUnitHeight = texture.height / 100.0f;

			// Масштабування, щоб відповідати розміру клітинки
			float scaleX = (MapConfig.CellSize / selectedBuilding.Weight) / textureUnitWidth;
			float scaleY = (MapConfig.CellSize / selectedBuilding.Height) / textureUnitHeight;

			// Застосування масштабу до об'єкта
			newBuildingObject.transform.localScale = new Vector3(scaleX * MapConfig.CellSize, scaleY * MapConfig.CellSize, 1);
			// Add a BoxCollider2D for interaction
			newBuildingObject.AddComponent<BoxCollider2D>();
		}
	}
}
