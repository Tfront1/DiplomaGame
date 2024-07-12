using GameUtilities;
using GameUtilities.Utils;
using UnityEngine;

public class TestingScript : MonoBehaviour
{
	[SerializeField]
	private TilemapVisual _tilemapVisual;
	private Tilemap _tilemap;
	private TilemapSprite _tilemapSprite;

	private void Start()
	{
		_tilemap = new Tilemap(MapConfig.MapWidth, MapConfig.MapHeight, MapConfig.SellSize, new Vector3(MapConfig.MapStartPointX, MapConfig.MapStartPointY));

		_tilemap.SetTilemapVisual(_tilemapVisual);
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			_tilemap.SetTilemapSprite(UtilsClass.GetMouseWorldPosition(), _tilemapSprite);
		}

		if (Input.GetKeyDown(KeyCode.F1)) 
		{
			_tilemapSprite = TilemapSprite.None;
			CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
		if (Input.GetKeyDown(KeyCode.F2))
		{
			_tilemapSprite = TilemapSprite.Ground;
			CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
		if (Input.GetKeyDown(KeyCode.F3))
		{
			_tilemapSprite = TilemapSprite.Dirt;
			CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
		if (Input.GetKeyDown(KeyCode.F4))
		{
			_tilemapSprite = TilemapSprite.Sand;
			CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
	}
}