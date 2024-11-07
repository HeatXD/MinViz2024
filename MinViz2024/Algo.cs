using System.Numerics;

namespace MinViz2024
{
    internal class Algo
    {
        public enum ResultType
        {
            NNH,
            ACO
        }

        public struct Result
        {
            public ResultType AlgoUsed;
            public List<Vector3> Points;
            public double[,]? DistanceMatrix;
            public double[,]? PheromoneMatrix;
            public List<double> Distances;
            public List<List<int>> Solutions;
            public DateTime ResultTime;

            public Result(ResultType rtype)
            {
                AlgoUsed = rtype;
                Points = new List<Vector3>();
                Distances = new List<double>();
                Solutions = new List<List<int>>();
                ResultTime = DateTime.UtcNow;
            }
        }

        public class Sector
        {
            public Vector3 Center;
            public Vector3 Size;
            public Vector3 Min => Center - (Size * 0.5f);
            public Vector3 Max => Center + (Size * 0.5f);

            private readonly Random _random;

            public Sector(Vector3 center, Vector3 size, int? seed = null)
            {
                Center = center;
                Size = size;

                _random = seed.HasValue ? new Random(seed.Value) : new Random(); 
            }

            public List<Vector3> CreateRandomPositions(int num = 1)
            {
                var result = new List<Vector3>();

                float x, y, z;

                for (int i = 0; i < num; i++)
                {
                    x = (float)(_random.NextDouble() * Size.X) + Min.X;
                    y = (float)(_random.NextDouble() * Size.Y) + Min.Y;
                    z = (float)(_random.NextDouble() * Size.Z) + Min.Z;

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
