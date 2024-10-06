using System.Collections.Generic;

[System.Serializable]
public class TerrainTexturesDto
{
	public string TexturesPath;
	public int DefaultTextureSize;
	public List<TerrainTexturesSpriteDto> TilemapSprites;
}