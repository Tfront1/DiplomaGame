using System.Collections.Generic;

public static class BuildingTexturesConfig
{
    public static string TexturesPath { get; set; }
    public static int DefaultTextureSize { get; set; }
    public static List<BuildingTexture> BuildingTexture { get; set; } = new();
}
