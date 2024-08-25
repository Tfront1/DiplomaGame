namespace Biomes
{
	internal static class SeedRandom

	{
		static int _seed;
		static int _mod = 1000000007;
		static long _cur;
		internal static void SetSeed(int newSeed)
		{
			_seed = newSeed;
			_cur = _seed % _mod;
			_cur += 17;
			_cur *= (_cur + 13);
			_cur += 1003 * _seed;
			_cur %= _mod;
		}

		internal static int Get(int x, int y)
		{
			long ret = x * x + 4 * y + _seed + 997;
			ret %= _mod;
			ret *= x ^ _seed + y ^ _seed + 7;
			ret += 3 * _seed + 17 * y + _seed * _seed + 10007;
			ret %= _mod;

			return (int)ret;
		}
	}
}
