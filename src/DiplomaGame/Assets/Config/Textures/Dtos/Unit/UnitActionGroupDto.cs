using System.Collections.Generic;

[System.Serializable]
public class UnitActionGroupDto
{
    public int ActionId;
    public string ActionName;
    public string ActionFolder;
    public List<UnitTextureDto> Textures;
}