﻿using System.Numerics;

namespace MinViz2024
{
    internal class ACO
    {
        private readonly List<Vector3> points;
        private readonly double[,] pheromoneMatrix;
        private readonly double[,] distanceMatrix;
        private readonly int numAnts;
        private readonly double evaporationRate;
        private readonly double alpha;
        private readonly double beta;
        private readonly Random random;

        public ACO(List<Vector3> points, int? seed = null, int numAnts = 15,
            double evaporationRate = 0.1, double alpha = 1, double beta = 2)
        {
            this.points = points;
            this.numAnts = numAnts;
            this.evaporationRate = evaporationRate;
            this.alpha = alpha;
            this.beta = beta;
            this.random = seed.HasValue ? new Random(seed.Value) : new Random();

            int num = points.Count;
            pheromoneMatrix = new double[num, num];
            distanceMatrix = new double[num, num];

            // Initialize distance and pheromone matrices
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    distanceMatrix[i, j] = Algo.SquaredDistanceTo(points[i], points[j]);
                    pheromoneMatrix[i, j] = 1.0; // Initial pheromone level
                }
            }
        }

        public List<int> Solve(int maxIterations = 100)
        {
            var bestTour = new List<int>();
            double bestTourLength = double.MaxValue;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var antTours = new List<List<int>>();
                var antTourLengths = new List<double>();

                // Construct solutions for each ant
                for (int ant = 0; ant < numAnts; ant++)
                {
                    var tour = ConstructSolution();
                    double tourLength = CalculateTourLength(tour);
                    antTours.Add(tour);
                    antTourLengths.Add(tourLength);

                    if (tourLength < bestTourLength)
                    {
                        bestTourLength = tourLength;
                        bestTour = new List<int>(tour);
                    }
                }

                UpdatePheromones(antTours, antTourLengths);
            }

            return bestTour;
        }

        private List<int> ConstructSolution()
        {
            int n = points.Count;
            var visited = new bool[n];
            var tour = new List<int>();

            // Start from random city
            int current = random.Next(n);
            tour.Add(current);
            visited[current] = true;

            while (tour.Count < n)
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
            int n = points.Count;
            var probabilities = new double[n];
            double total = 0;

            // Calculate probabilities for unvisited cities
            for (int i = 0; i < n; i++)
            {
                if (!visited[i])
                {
                    probabilities[i] = Math.Pow(pheromoneMatrix[current, i], alpha) *
                                     Math.Pow(1.0 / distanceMatrix[current, i], beta);
                    total += probabilities[i];
                }
            }

            // Select next city using roulette wheel selection
            double r = random.NextDouble() * total;
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                if (!visited[i])
                {
                    sum += probabilities[i];
                    if (sum >= r)
                        return i;
                }
            }

            // Fallback: return first unvisited city
            return Array.FindIndex(visited, v => !v);
        }

        private void UpdatePheromones(List<List<int>> antTours, List<double> tourLengths)
        {
            int n = points.Count;

            // Evaporation
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    pheromoneMatrix[i, j] *= (1 - evaporationRate);

            // Add new pheromones
            for (int ant = 0; ant < antTours.Count; ant++)
            {
                double contribution = 1.0 / tourLengths[ant];
                var tour = antTours[ant];

                for (int i = 0; i < tour.Count - 1; i++)
                {
                    int city1 = tour[i];
                    int city2 = tour[i + 1];
                    pheromoneMatrix[city1, city2] += contribution;
                    pheromoneMatrix[city2, city1] += contribution;
                }

                // Connect last city to first
                int first = tour[0];
                int last = tour[^1];
                pheromoneMatrix[last, first] += contribution;
                pheromoneMatrix[first, last] += contribution;
            }
        }

        private double CalculateTourLength(List<int> tour)
        {
            double length = 0;
            for (int i = 0; i < tour.Count - 1; i++)
            {
                length += distanceMatrix[tour[i], tour[i + 1]];
            }
            length += distanceMatrix[tour[^1], tour[0]]; // Return to start
            return length;
        }
    }
}
