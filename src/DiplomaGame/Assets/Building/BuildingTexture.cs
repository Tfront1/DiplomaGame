using UnityEngine;

public class BuildingTexture
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public Texture2D Texture { get; set; }
    public int VisualWidthCell { get; set; }
    public int VisualHeightCell { get; set; }
    public float Scale { get; set; }
    public bool RandomPos { get; set; }
}
