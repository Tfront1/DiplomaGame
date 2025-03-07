using System.Collections.Generic;
using Supplies;

public static class SupplyTexturesConfig
{
    public static string TexturesPath { get; set; }
    public static int DefaultTextureSize { get; set; }
    public static List<SupplyTexture> SupplyTextures { get; set; } = new();
} 
