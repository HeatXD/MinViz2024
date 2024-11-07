using System.Diagnostics;
using System.Numerics;

namespace MinViz2024
{
    internal class NNH
    {
        private readonly List<Vector3> _points;
        private readonly double[,] _distanceMatrix;

        public NNH(List<Vector3> points)
        {
            _points = points;

            // pre calculate distances
            int num = _points.Count;
            _distanceMatrix = new double[num, num];


            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    _distanceMatrix[i, j] = Algo.SquaredDistanceTo(_points[i], _points[j]);
                }
            }
        }

        public Algo.Result FullSolve()
        {
            var algoResult = new Algo.Result(Algo.ResultType.NNH);

            algoResult.Points = _points;
            algoResult.DistanceMatrix = _distanceMatrix;

            double bestDistance = double.MaxValue;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < _points.Count; i++)
            {
                var result = StepSolve(i);
                var tourLength = CalculateTourLength(result);

                if (tourLength < bestDistance)
                {
                    bestDistance = tourLength;

                    algoResult.Distances.Add(bestDistance);
                    algoResult.Solutions.Add(result);
                    algoResult.ElapsedTimes.Add(stopwatch.ElapsedTicks);
                }
            }

            return algoResult;
        }

        private List<int> StepSolve(int idx)
        {
            int n = _points.Count;
            // If no start index provided, choose random start point
            int start = idx; 

            var tour = new List<int> { start };
            var visited = new bool[n];
            visited[start] = true;

            while (tour.Count < n)
            {
                int current = tour[^1];
                int nearest = -1;
                double minDistance = double.MaxValue;

                // Find nearest unvisited point
                for (int i = 0; i < n; i++)
                {
                    if (!visited[i] && _distanceMatrix[current, i] < minDistance)
                    {
                        minDistance = _distanceMatrix[current, i];
                        nearest = i;
                    }
                }

                tour.Add(nearest);
                visited[nearest] = true;
            }

            return tour;
        }

        private double CalculateTourLength(List<int> tour)
        {
            double length = 0;
            for (int i = 0; i < tour.Count - 1; i++)
            {
                length += _distanceMatrix[tour[i], tour[i + 1]];
            }
            length += _distanceMatrix[tour[^1], tour[0]]; // Return to start
            return length;
        }
    }
}
