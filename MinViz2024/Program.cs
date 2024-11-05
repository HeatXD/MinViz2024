using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;

namespace MinViz2024
{
    internal class Program
    {
        static void Main(string[] args)
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

            float radius = 0.025f;

            rlImGui.Setup(true);

            // Set initial window positions and sizes
            ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

            // Window states
            bool is3DViewportOpen = true;
            bool isSceneControlsOpen = true;
            bool isAlgorithmControlOpen = true;

            // 3d scene state
            bool showGrid = true;
            int selectedSector = 0;
            int numberOfPoints = 0;

            // sectors
            var sectors = new List<Algo.Sector>();

            // points
            var points = new List<Vector3>();

            while (!Raylib.WindowShouldClose())
            {
                // First render the 3D scene to the render texture
                Raylib.BeginTextureMode(renderTexture);
                Raylib.ClearBackground(Color.Black);

                Raylib.BeginMode3D(camera);

                if (showGrid) {
                    Raylib.DrawGrid(50, 1.0f);
                }

                for (int i = 0; i < sectors.Count; i++) {
                    Raylib.DrawCubeWiresV(sectors[i].Center, sectors[i].Size, Color.Yellow);
                }

                for (int i = 0; i < points.Count; i++) {
                    Raylib.DrawSphere(points[i], radius, Color.Blue);
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
                    }
                }
                ImGui.End();


                // Controls window with fixed size
                ImGui.SetNextWindowPos(new Vector2(screenWidth - 310, 170), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new Vector2(300, 150), ImGuiCond.FirstUseEver);

                ImGuiWindowFlags sceneControlFlags = ImGuiWindowFlags.None;

                if (ImGui.Begin("Scene Controls", ref isSceneControlsOpen, sceneControlFlags))
                {
                    // Window control buttons
                    if (ImGui.Button("Reset Window Positions"))
                    {
                        ImGui.SetWindowPos("3D View", new Vector2(10, 10));
                        ImGui.SetWindowSize("3D View", new Vector2(800, 600));
                        ImGui.SetWindowPos("Scene Controls", new Vector2(screenWidth - 310, 10));
                        ImGui.SetWindowSize("Scene Controls", new Vector2(300, 150));
                        ImGui.SetWindowPos("Algorithm Controls", new Vector2(screenWidth - 460, 170));
                        ImGui.SetWindowSize("Algorithm Controls", new Vector2(450, 400));
                    }

                    if (ImGui.Button("Toggle Grid"))
                    {
                        showGrid = !showGrid;
                    }
                }
                ImGui.End();


                // Controls window with fixed size
                ImGui.SetNextWindowPos(new Vector2(screenWidth - 460, 170), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new Vector2(450, 400), ImGuiCond.FirstUseEver);

                ImGuiWindowFlags controlFlags = ImGuiWindowFlags.None;

                if (ImGui.Begin("Algorithm Controls", ref isAlgorithmControlOpen, controlFlags))
                {
                    for (int i = 0; i < sectors.Count; i++)
                    {
                        ImGui.InputFloat3($"Sector[{i}] Position", ref sectors[i].Center);
                        ImGui.InputFloat3($"Sector[{i}] Size", ref sectors[i].Size);
                    }

                    if (ImGui.Button("Add Sector"))
                    {
                        sectors.Add(new Algo.Sector(Vector3.Zero, new Vector3(1, 1, 1)));
                    }

                    if (ImGui.Button("Remove Sector") && sectors.Count > 0)
                    {
                        sectors.RemoveAt(sectors.Count - 1);
                    }

                    ImGui.InputInt("Selected Sector", ref selectedSector);
                    ImGui.InputInt("Number of Points", ref numberOfPoints);
                    if (ImGui.Button("Generate Points") && selectedSector > -1 && selectedSector < sectors.Count && numberOfPoints > 0)
                    {
                        points.AddRange(sectors[selectedSector].CreateRandomPositions(numberOfPoints));
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
    }
}