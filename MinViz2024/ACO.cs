using System.Numerics;

namespace MinViz2024
{
    internal class ACO
    {
        private readonly List<Vector3> _points;
        private readonly double[,] _pheromoneMatrix;
        private readonly double[,] _distanceMatrix;
        private readonly int _numAnts;
        private readonly double _evaporationRate;
        private readonly double _alpha;
        private readonly double _beta;
        private readonly Random _random;

        public ACO(List<Vector3> points, int? seed = null, int numAnts = 15,
            double evaporationRate = 0.1, double alpha = 1, double beta = 2)
        {
            _points = points;
            _numAnts = numAnts;
            _evaporationRate = evaporationRate;
            _alpha = alpha;
            _beta = beta;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();

            int num = points.Count;
            _pheromoneMatrix = new double[num, num];
            _distanceMatrix = new double[num, num];

            // Initialize distance and pheromone matrices
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    _distanceMatrix[i, j] = Algo.SquaredDistanceTo(points[i], points[j]);
                    _pheromoneMatrix[i, j] = 1.0; // Initial pheromone level
                }
            }
        }

        public Algo.Result Solve(int maxIterations = 100)
        {
            var result = new Algo.Result(Algo.ResultType.ACO);

            result.Points = _points;
            result.PheromoneMatrix = _pheromoneMatrix;
            result.DistanceMatrix = _distanceMatrix;

            double bestTourLength = double.MaxValue;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var antTours = new List<List<int>>();
                var antTourLengths = new List<double>();

                // Construct solutions for each ant
                for (int ant = 0; ant < _numAnts; ant++)
                {
                    var tour = ConstructSolution();
                    double tourLength = CalculateTourLength(tour);
                    antTours.Add(tour);
                    antTourLengths.Add(tourLength);

                    if (tourLength < bestTourLength)
                    {
                        result.Distances.Add(tourLength);
                        result.Solutions.Add(tour);

                        bestTourLength = tourLength;
                    }
                }

                UpdatePheromones(antTours, antTourLengths);
            }

            return result;
        }

        private List<int> ConstructSolution()
        {
            int num = _points.Count;
            var visited = new bool[num];
            var tour = new List<int>();

            // Start from random city
            int current = _random.Next(num);
            tour.Add(current);
            visited[current] = true;

            while (tour.Count < num)
            {
                int next = SelectNextCity(current, visited);
                tour.Add(next);
                visited[next] = true;
                current = next;
            }

            return tour;
        }

        private int SelectNextCity(int current, bool[] visited)
        {
            int num = _points.Count;
            var probabilities = new double[num];
            double total = 0;

            // Calculate probabilities for unvisited cities
            for (int i = 0; i < num; i++)
            {
                if (!visited[i])
                {
                    probabilities[i] = Math.Pow(_pheromoneMatrix[current, i], _alpha) *
                                     Math.Pow(1.0 / _distanceMatrix[current, i], _beta);
                    total += probabilities[i];
                }
            }

            // Select next city using roulette wheel selection
            double r = _random.NextDouble() * total;
            double sum = 0;
            for (int i = 0; i < num; i++)
            {
                if (!visited[i])
                {
                    sum += probabilities[i];
                    if (sum >= r)
                    {
                        return i;
                    }
                }
            }

            // Fallback: return first unvisited city
            return Array.FindIndex(visited, v => !v);
        }

        private void UpdatePheromones(List<List<int>> antTours, List<double> tourLengths)
        {
            int n = _points.Count;

            // Evaporation
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    _pheromoneMatrix[i, j] *= (1 - _evaporationRate);
                }
            }
            // Add new pheromones
            for (int ant = 0; ant < antTours.Count; ant++)
            {
                double contribution = 1.0 / tourLengths[ant];
                var tour = antTours[ant];

                for (int i = 0; i < tour.Count - 1; i++)
                {
                    int city1 = tour[i];
                    int city2 = tour[i + 1];
                    _pheromoneMatrix[city1, city2] += contribution;
                    _pheromoneMatrix[city2, city1] += contribution;
                }

                // Connect last city to first
                int first = tour[0];
                int last = tour[^1];
                _pheromoneMatrix[last, first] += contribution;
                _pheromoneMatrix[first, last] += contribution;
            }
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
