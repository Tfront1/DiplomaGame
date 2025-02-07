using System.Collections.Generic;
using Supplies;

public static class SupplyTexturesConfig
{
    public static string TexturesPath { get; set; }
    public static List<SupplyTexturesSpriteDto> SupplyTexturesSprite { get; set; }
    public static int DefaultTextureSize { get; set; }
    public static List<SupplyTexture> SupplyTextures = new();
} 
