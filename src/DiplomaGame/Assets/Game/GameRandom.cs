using System;

namespace Game
{
    public static class GameRandom
    {
        public static Random Random { get; set; }
        public static int Seed { get; set; }

        public static void SetSeed(int seed)
        {
            Seed = seed;
            Random = new Random(Seed);
        }
    }
}
