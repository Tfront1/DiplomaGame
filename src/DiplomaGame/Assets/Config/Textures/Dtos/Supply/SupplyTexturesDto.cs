using System.Collections.Generic;

[System.Serializable]
public class SupplyTexturesDto
{
    public string TexturesPath;
    public int DefaultTextureSize;
    public List<SupplyTexturesSpriteDto> TilemapSprites;
}