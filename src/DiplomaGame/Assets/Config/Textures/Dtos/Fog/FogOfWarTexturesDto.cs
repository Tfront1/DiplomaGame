using System.Collections.Generic;

[System.Serializable]
public class FogOfWarTexturesDto
{
    public string FolderPath;
    public FogOfWarTextureDto Background;
    public List<FogOfWarTextureDto> Clouds;
}
