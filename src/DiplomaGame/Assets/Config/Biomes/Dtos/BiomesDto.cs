using System.Collections.Generic;

[System.Serializable]
public class BiomesDto
{
	public float NoiseMult;
	public float NoiseDist;
	public int BiomeRange;
	public int ValidationWeightPercentageDifference;
	public List<BiomeDto> Biomes;
}
