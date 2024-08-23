using System.Collections.Generic;

public static class BiomesConfig
{
    public static float NoiseMult;
    public static float NoiseDist;
    public static List<Biome> BiomesList = new();
    public static int BiomeRange;

    public class Biome
    {
        public int Id;
        public string Name;
        public float Weight;
    }
}
