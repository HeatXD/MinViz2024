using System.Numerics;

namespace MinViz2024
{
    internal class Algo
    {
        public class Sector
        {
            public Vector3 Center;
            public Vector3 Size;
            public Vector3 Min => Center - (Size * 0.5f);
            public Vector3 Max => Center + (Size * 0.5f);

            private Random random;

            public Sector(Vector3 center, Vector3 size, int? seed = null)
            {
                Center = center;
                Size = size;

                random = seed.HasValue ? new Random(seed.Value) : new Random(); 
            }

            public List<Vector3> CreateRandomPositions(int num = 1)
            {
                var result = new List<Vector3>();

                float x, y, z;

                for (int i = 0; i < num; i++)
                {
                    x = (float)(random.NextDouble() * Size.X) + Min.X;
                    y = (float)(random.NextDouble() * Size.Y) + Min.Y;
                    z = (float)(random.NextDouble() * Size.Z) + Min.Z;

                    result.Add(new Vector3(x, y, z));
                }

                return result;
            }
        }

        public static double SquaredDistanceTo(Vector3 f, Vector3 t)
        {
            return Math.Pow(f.X - t.X, 2) +
                   Math.Pow(f.Y - t.Y, 2) +
                   Math.Pow(f.Z - t.Z, 2);
        }
    }
}
