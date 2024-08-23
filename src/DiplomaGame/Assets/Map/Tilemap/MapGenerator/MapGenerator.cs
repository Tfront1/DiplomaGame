using static BiomesConfig;

public class MapGenerator
{
	private int _mapHeight;
    private int _mapWidth;

    private float _noiseMult;
    private float _noiseDist;
    private int _biomeRange;


    private int _seed;

    private BiomeType[] _biomes;

    public MapGenerator(int seed)
    {
        _seed = seed;

		LoadVariables();
    }

	public int[,] GenerateMap()
	{
		return Noise.GenerateBiomeMap(_mapWidth, _mapHeight, _seed, _biomeRange, _biomes, _noiseMult, _noiseDist);
	}

    private void LoadVariables()
    {
        _mapHeight = MapConfig.MapHeight;
		_mapWidth = MapConfig.MapWidth;

        _noiseMult = NoiseMult;
        _noiseDist = NoiseDist;
        _biomeRange = BiomeRange;

        _biomes = new BiomeType[BiomesList.Count];

        for (var i = 0; i < _biomes.Length; i++)
        {
            _biomes[i] = new BiomeType
            {
                _biomeId = BiomesList[i].Id,
                _weight = BiomesList[i].Weight
            };
        }
    }
}

public class BiomeType
{
    public int _biomeId;
    public float _weight;
}
