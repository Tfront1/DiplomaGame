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

        public static Vector2Int GetRandomCoords(Vector2Int nearCoords, int maxDistance)
        {
            Vector2Int result;
            var currentMaxDistance = maxDistance;
            var attempts = 0;
            var maxAttempts = 20;

            do
            {
                if (attempts > 0 && attempts % maxAttempts == 0)
                {
                    currentMaxDistance += 1;
                    if (currentMaxDistance > maxDistance * 3)
                    {
                        return nearCoords;
                    }
                }

                var offsetX = Random.Next(-currentMaxDistance, currentMaxDistance + 1);
                var offsetY = Random.Next(-currentMaxDistance, currentMaxDistance + 1);

                result = new Vector2Int(
                    nearCoords.x + offsetX,
                    nearCoords.y + offsetY
                );

                attempts++;
            } while (!GridService.IsWorldPositionInMapBounds(result));

            return result;
        }

        public static Vector2Int GetRandomCoords(Vector2Int nearCoords, int maxDistance, int width, int height)
        {
            Vector2Int result;
            var currentMaxDistance = maxDistance;
            var attempts = 0;
            var maxAttempts = 20;

            do
            {
                if (attempts > 0 && attempts % maxAttempts == 0)
                {
                    currentMaxDistance += 1;
                    if (currentMaxDistance > maxDistance * 3)
                    {
                        return nearCoords;
                    }
                }

                var offsetX = Random.Next(-currentMaxDistance, currentMaxDistance + 1);
                var offsetY = Random.Next(-currentMaxDistance, currentMaxDistance + 1);

                result = new Vector2Int(
                    nearCoords.x + offsetX,
                    nearCoords.y + offsetY
                );

                attempts++;

            } while (!GridService.IsWorldPositionInMapBounds(result, width, height));

            return result;
        }
    }
}
