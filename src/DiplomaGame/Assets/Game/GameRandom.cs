using UnityEngine;

namespace Game
{
    public static class GameRandom
    {
        public static System.Random Random { get; set; }
        public static int Seed { get; set; }

        public static void SetSeed(int seed)
        {
            Seed = seed;
            Random = new System.Random(Seed);
        }

        public static Vector2Int GetRandomCoords()
        {
            Vector2Int result;

            do
            {
                result = new Vector2Int(Random.Next(MapConfig.MapWidth + 1), Random.Next(MapConfig.MapHeight + 1));
            } while (!GridService.IsWorldPositionInMapBounds(result));

            return result;
        }
    }
}
