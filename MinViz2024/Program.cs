using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace MinViz2024
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Write 'sim' to enter simulation mode otherwise write anything to run the benchmarks");
            var choice = Console.ReadLine();
            if (choice != null && choice.ToLower().Contains("sim"))
            {
                RunSim();
            }
            else
            {
                Console.WriteLine("Bench Mode");
                RunBenchmark();
                
            }
        }

        private static void RunSim()
        {
            // Initialize main window
            int screenWidth = 1280;
            int screenHeight = 720;
            Raylib.InitWindow(screenWidth, screenHeight, "Minor 2024 Jamie Meyer");
            Raylib.SetTargetFPS(60);

            // Create a render texture to draw the 3D scene
            RenderTexture2D renderTexture = Raylib.LoadRenderTexture(800, 600);

            Camera3D camera = new Camera3D
            {
                Position = new Vector3(5.0f, 5.0f, 5.0f),
                Target = new Vector3(0.0f, 0.0f, 0.0f),
                Up = new Vector3(0.0f, 1.0f, 0.0f),
                FovY = 45.0f,
                Projection = CameraProjection.Perspective
            };

            float radius = 0.04f;

            rlImGui.Setup(true);

            // Set initial window positions and sizes
            ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

            // Window states
            bool is3DViewportOpen = true;
            bool isSceneControlsOpen = true;
            bool isAlgorithmControlOpen = true;
            bool isAlgorithmResultOpen = false;

            // 3d scene state
            bool showGrid = true;
            int selectedSector = 0;
            int numberOfPoints = 0;
            int Seed = new Random().Next();

            // sectors
            var sectors = new List<Algo.Sector>();
            var selected = new List<bool>();

            // points
            var points = new Dictionary<int, List<Vector3>>();

            // algos
            var selectedAlgos = new bool[2] { false, false };
            int solutionsPerSecond = 1;

            NNH NNH;

            int ACOSeed = new Random().Next();
            int ACOAntCount = 15;
            int ACOIterations = 100;
            double ACOEvapRate = 0.1;
            double ACOAlpha = 1;
            double ACOBeta = 2;
            ACO ACO;

            // results
            var results = new List<Algo.Result>();
            int showIdx = -1;
            int replayIdx = -1;

            while (!Raylib.WindowShouldClose())
            {
                // First render the 3D scene to the render texture
                Raylib.BeginTextureMode(renderTexture);
                Raylib.ClearBackground(Color.Black);

                Raylib.BeginMode3D(camera);

                if (showGrid)
                {
                    Raylib.DrawGrid(25, 1.0f);
                }

                for (int i = 0; i < sectors.Count; i++)
                {
                    Raylib.DrawCubeWiresV(sectors[i].Center, sectors[i].Size, selected[i] ? Color.Magenta : Color.Yellow);
                }

                for (int i = 0; i < points.Count; i++)
                {
                    for (int j = 0; j < points[i].Count; j++)
                    {
                        Raylib.DrawSphere(points[i][j], radius, selected[i] ? Color.Gold : Color.Blue);
                    }
                }

                if (showIdx >= 0 && showIdx < results.Count)
                {
                    var best = results[showIdx].Solutions.Last();
                    for (int i = 0; i < best.Count - 1; i++)
                    {
                        Raylib.DrawLine3D(
                            results[showIdx].Points[best[i]],
                            results[showIdx].Points[best[i + 1]],
                            Color.Green);
                    }
                    // return to start
                    Raylib.DrawLine3D(results[showIdx].Points[best[0]], results[showIdx].Points[best[^1]], Color.Green);
                }

                Raylib.EndMode3D();

                Raylib.EndTextureMode();

                // Now draw everything to the screen
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.DarkGray);

                rlImGui.Begin();

                // 3D View window with size constraints
                ImGui.SetNextWindowSizeConstraints(
                    new Vector2(200, 200),    // Minimum size
                    new Vector2(screenWidth - 20, screenHeight - 20)  // Maximum size
                );

                ImGuiWindowFlags viewportFlags = ImGuiWindowFlags.None;

                if (ImGui.Begin("3D View", ref is3DViewportOpen, viewportFlags))
                {
                    Vector2 windowSize = ImGui.GetContentRegionAvail();

                    // Display the render texture in the ImGui window
                    ImGui.Image(
                        (IntPtr)renderTexture.Texture.Id,
                        windowSize,
                        new Vector2(0, 1),
                        new Vector2(1, 0)
                    );

                    if (ImGui.IsWindowFocused())
                    {
                        if (Raylib.IsKeyDown(KeyboardKey.Right)) camera.Position.X += 0.1f;
                        if (Raylib.IsKeyDown(KeyboardKey.Left)) camera.Position.X -= 0.1f;
                        if (Raylib.IsKeyDown(KeyboardKey.Up)) camera.FovY -= 0.1f;
                        if (Raylib.IsKeyDown(KeyboardKey.Down)) camera.FovY += 0.1f;
                    }
                }
                ImGui.End();

                // Controls window with fixed size
                ImGui.SetNextWindowPos(new Vector2(10, 620), ImGuiCond.Once);
                ImGui.SetNextWindowSize(new Vector2(300, 90), ImGuiCond.Once);

                ImGuiWindowFlags sceneControlFlags = ImGuiWindowFlags.None;

                if (ImGui.Begin("Scene Controls", ref isSceneControlsOpen, sceneControlFlags))
                {
                    // Window control buttons
                    if (ImGui.Button("Reset Window Positions"))
                    {
                        ImGui.SetWindowPos("3D View", new Vector2(10, 10));
                        ImGui.SetWindowSize("3D View", new Vector2(800, 600));
                        ImGui.SetWindowPos("Scene Controls", new Vector2(10, 620));
                        ImGui.SetWindowSize("Scene Controls", new Vector2(300, 90));
                        ImGui.SetWindowPos("Algorithm Controls", new Vector2(screenWidth - 460, 10));
                        ImGui.SetWindowSize("Algorithm Controls", new Vector2(450, 560));
                        ImGui.SetWindowPos("Algorithm Results", new Vector2(screenWidth - 460, 580));
                        ImGui.SetWindowSize("Algorithm Results", new Vector2(450, 130));
                    }

                    if (ImGui.Button("Toggle Grid"))
                    {
                        showGrid = !showGrid;
                    }
                }
                ImGui.End();

                // Controls window with fixed size
                ImGui.SetNextWindowPos(new Vector2(screenWidth - 460, 10), ImGuiCond.Once);
                ImGui.SetNextWindowSize(new Vector2(450, 560), ImGuiCond.Once);

                ImGuiWindowFlags controlFlags = ImGuiWindowFlags.None;

                if (ImGui.Begin("Algorithm Controls", ref isAlgorithmControlOpen, controlFlags))
                {
                    ImGui.SeparatorText("Sector Control");
                    for (int i = 0; i < sectors.Count; i++)
                    {
                        ImGui.InputFloat3($"Sector[{i}] Position", ref sectors[i].Center);
                        ImGui.InputFloat3($"Sector[{i}] Size", ref sectors[i].Size);
                    }

                    ImGui.Separator();
                    ImGui.InputInt("Next Sector Seed", ref Seed);
                    if (ImGui.Button("Add Sector"))
                    {
                        sectors.Add(new Algo.Sector(Vector3.Zero, new Vector3(1, 1, 1), Seed));
                        points.Add(sectors.Count - 1, new List<Vector3>());
                        selected.Add(false);
                    }

                    if (ImGui.Button("Remove Last Sector") && sectors.Count > 0)
                    {
                        int remIdx = sectors.Count - 1;
                        sectors.RemoveAt(remIdx);
                        points.Remove(remIdx);
                        selected.RemoveAt(remIdx);
                    }

                    ImGui.SeparatorText("Sector Point Spawning");
                    ImGui.InputInt("Selected Sector", ref selectedSector);
                    ImGui.InputInt("Number of Points", ref numberOfPoints);
                    if (ImGui.Button("Generate Points") && selectedSector > -1 && selectedSector < sectors.Count && numberOfPoints > 0)
                    {
                        points[selectedSector].AddRange(sectors[selectedSector].CreateRandomPositions(numberOfPoints));
                    }

                    ImGui.SeparatorText("Available Sectors for Computation");
                    for (int i = 0; i < selected.Count; i++)
                    {
                        bool select = selected[i];
                        if (ImGui.Checkbox($"Sector {i}", ref select))
                        {
                            selected[i] = select;
                        }

                        if (i + 1 % 4 != 0 && i + 1 != selected.Count)
                        {
                            ImGui.SameLine();
                        }
                    }

                    ImGui.SeparatorText("Available Algoritms");

                    for (int i = 0; i < selectedAlgos.Length; i++)
                    {
                        ImGui.Checkbox(i == 0 ? "Nearest Neighbor Heuristic" : "Ant Colony Optimization", ref selectedAlgos[i]);
                        if (i + 1 != selectedAlgos.Length)
                        {
                            ImGui.SameLine();
                        }
                    }

                    if (selectedAlgos.Any(x => x == true) && selected.Any(x => x == true))
                    {
                        // If ACO is selected show options
                        if (selectedAlgos[1])
                        {
                            ImGui.SeparatorText("ACO Settings");
                            ImGui.InputInt("Seed", ref ACOSeed);
                            ImGui.InputInt("Ant Count", ref ACOAntCount);
                            ImGui.InputInt("Max Iterations", ref ACOIterations);
                            ImGui.InputDouble("Alpha", ref ACOAlpha);
                            ImGui.InputDouble("Beta", ref ACOBeta);
                            ImGui.InputDouble("Evaporation Rate", ref ACOEvapRate);
                        }

                        if (ImGui.Button("Run Algorithms"))
                        {
                            // fetch all selected positions
                            var nodes = new List<Vector3>();
                            for (int i = 0; i < points.Count; i++)
                            {
                                if (!selected[i])
                                {
                                    continue;
                                }

                                for (int j = 0; j < points[i].Count; j++)
                                {
                                    nodes.Add(points[i][j]);
                                }
                            }

                            // DO NNH
                            if (selectedAlgos[0])
                            {
                                NNH = new NNH(nodes);
                                results.Add(NNH.FullSolve());
                            }

                            // DO ACO
                            if (selectedAlgos[1])
                            {
                                ACO = new ACO(nodes, ACOSeed, ACOAntCount, ACOEvapRate, ACOAlpha, ACOBeta);
                                results.Add(ACO.Solve(ACOIterations));
                            }
                        }
                    }
                }
                ImGui.End();

                // Controls window with fixed size
                ImGui.SetNextWindowPos(new Vector2(screenWidth - 460, 580), ImGuiCond.Once);
                ImGui.SetNextWindowSize(new Vector2(450, 130), ImGuiCond.Once);

                ImGuiWindowFlags resultFlags = ImGuiWindowFlags.None;

                if (ImGui.Begin("Algorithm Results", ref isAlgorithmResultOpen, resultFlags))
                {
                    ImGui.SeparatorText("Replay Settings");
                    ImGui.SliderInt("Replay Speed", ref solutionsPerSecond, 1, 30);
                    ImGui.Separator();

                    for (int i = results.Count - 1; i >= 0; i--)
                    {
                        var res = results[i];
                        if(res.Distances.Count < 1)
                        {
                            // cleanup dead results.
                            results.RemoveAt(i);
                            continue;
                        }

                        ImGui.Text($"{res.ResultTime.ToShortTimeString()} - {res.AlgoUsed} - {res.Distances.Last():F6}");
                        ImGui.SameLine();
                        if (ImGui.Button(showIdx != i ? $"Show[{i}]" : $"Hide[{i}]"))
                        {
                            showIdx = showIdx == i ? -1 : i;
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"Info[{i}]"))
                        {
                            Console.WriteLine("Info Test!");
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"Replay[{i}]"))
                        {
                            // disable show feature
                            showIdx = -1;
                            replayIdx = replayIdx == i ? -1 : i;
                            Console.WriteLine("Replay Test!");
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"X[{i}]"))
                        {
                            results.RemoveAt(i);
                        }
                        ImGui.Separator();
                    }
                }
                ImGui.End();

                rlImGui.End();
                Raylib.EndDrawing();
            }

            // Clean up
            Raylib.UnloadRenderTexture(renderTexture);
            rlImGui.Shutdown();
            Raylib.CloseWindow();
        }

        private static void RunBenchmark()
        {
            var AllBenchResults = new List<Algo.BenchResult>();
            var points = new List<Vector3>();

            Console.WriteLine("Test 1: 5x5x5 with 5 points");
            RunBenchmarkTest(points, 5, 5, 5, 5, ref AllBenchResults);

            Console.WriteLine("Test 2: 5x5x5 with 10 points");
            RunBenchmarkTest(points, 5, 5, 5, 10, ref AllBenchResults);

            Console.WriteLine("Test 3: 5x5x5 with 20 points");
            RunBenchmarkTest(points, 5, 5, 5, 20, ref AllBenchResults);

            Console.WriteLine("Test 3: 5x5x5 with 40 points");
            RunBenchmarkTest(points, 5, 5, 5, 40, ref AllBenchResults);

            Console.WriteLine("Test 3: 5x5x5 with 80 points");
            RunBenchmarkTest(points, 5, 5, 5, 80, ref AllBenchResults);

            Console.WriteLine("Test 4: 10x10x10 with 5 points");
            RunBenchmarkTest(points, 10, 10, 10, 5, ref AllBenchResults);

            Console.WriteLine("Test 5: 10x10x10 with 10 points");
            RunBenchmarkTest(points, 10, 10, 10, 10, ref AllBenchResults);

            Console.WriteLine("Test 6: 10x10x10 with 20 points");
            RunBenchmarkTest(points, 10, 10, 10, 20, ref AllBenchResults);

            Console.WriteLine("Test 6: 10x10x10 with 40 points");
            RunBenchmarkTest(points, 10, 10, 10, 40, ref AllBenchResults);

            Console.WriteLine("Test 6: 10x10x10 with 80 points");
            RunBenchmarkTest(points, 10, 10, 10, 80, ref AllBenchResults);

            Console.WriteLine("Test 7: 15x15x15 with 5 points");
            RunBenchmarkTest(points, 15, 15, 15, 5, ref AllBenchResults);

            Console.WriteLine("Test 8: 15x15x15 with 10 points");
            RunBenchmarkTest(points, 15, 15, 15, 10, ref AllBenchResults);

            Console.WriteLine("Test 9: 15x15x15 with 20 points");
            RunBenchmarkTest(points, 15, 15, 15, 20, ref AllBenchResults);

            Console.WriteLine("Test 9: 15x15x15 with 40 points");
            RunBenchmarkTest(points, 15, 15, 15, 40, ref AllBenchResults);

            Console.WriteLine("Test 9: 15x15x15 with 80 points");
            RunBenchmarkTest(points, 15, 15, 15, 80, ref AllBenchResults);

            WriteToCSV(AllBenchResults, "BenchResults.csv");
        }

        private static void RunBenchmarkTest(List<Vector3> points, int x, int y, int z, int numPoints, ref List<Algo.BenchResult> allBenchResults)
        {
            const int BENCH_ITERS = 20;
            for (int i = 0; i < BENCH_ITERS; i++)
            {
                int seed = i + BENCH_ITERS;
                var sector = new Algo.Sector(Vector3.Zero, new Vector3(x, y, z), seed);
                points.AddRange(sector.CreateRandomPositions(numPoints));

                // ACO
                var aco = new ACO(points, seed, points.Count);
                var acoResult = aco.Solve().AllBenches(seed, x * y * z, numPoints);
                allBenchResults.AddRange(acoResult);

                // NNH
                var nnh = new NNH(points);
                var nnhResult = nnh.FullSolve().AllBenches(seed, x * y * z, numPoints);
                allBenchResults.AddRange(nnhResult);

                // clear points
                points.Clear();
            }
        }

        static void WriteToCSV<T>(List<T> data, string filename)
        {
            // Get the public field names from the first object in the list
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            string[] columnNames = Array.ConvertAll(fields, f => f.Name);

            // Write the CSV file
            using (var writer = new StreamWriter(filename))
            {
                // Write the header row
                writer.WriteLine(string.Join(",", columnNames));

                // Write each object as a row
                foreach (var item in data)
                {
                    var values = new List<string>();
                    foreach (var field in fields)
                    {
                        var fieldValue = field.GetValue(item);
                        string formattedValue = "";

                        // Format the field value based on its data type
                        if (fieldValue is float)
                        {
                            formattedValue = ((float)fieldValue).ToString("F10", CultureInfo.InvariantCulture);
                        }
                        else if (fieldValue is double)
                        {
                            formattedValue = ((double)fieldValue).ToString("F10", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            formattedValue = fieldValue?.ToString() ?? "";
                        }

                        values.Add(formattedValue);
                    }
                    writer.WriteLine(string.Join(",", values));
                }
            }
        }
    }
}