using System.Collections.Generic;

[System.Serializable]
public class BuildingTexturesDto
{
    public string TexturesPath;
    public int DefaultTextureSize;
    public List<BuildingTexturesSpriteDto> TilemapSprites;
}
