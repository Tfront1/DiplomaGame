using System;

namespace GameUtilities.Utils.MapUtils
{
    public static class MapUtils
    {
        public static (int x, int y) GetRandomPosition(int width, int height, Random random)
        {
            return (random.Next(width), random.Next(height));
        }

        
    }
}