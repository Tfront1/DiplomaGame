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
		_tilemapSprite = TilemapData._tilemapSprites[0];

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
            _tilemapSprite = TilemapData._tilemapSprites[1];

            CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
		if (Input.GetKeyDown(KeyCode.F2))
		{
            _tilemapSprite = TilemapData._tilemapSprites[2];
            CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
		if (Input.GetKeyDown(KeyCode.F3))
		{
            _tilemapSprite = TilemapData._tilemapSprites[3];
            CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
		if (Input.GetKeyDown(KeyCode.F4))
		{
            _tilemapSprite = TilemapData._tilemapSprites[0];
			CMDebug.TextPopupMouse(_tilemapSprite.ToString());
		}
	}
}