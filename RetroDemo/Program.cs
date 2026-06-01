using Raylib_cs;
using RetroDemo.Scenes;

int monitor = 0;
int screenWidth = Raylib.GetMonitorWidth(monitor);
int screenHeight = Raylib.GetMonitorHeight(monitor);

// Enable anti-aliasing, vsync, and fullscreen before the window is created
Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.FullscreenMode);
Raylib.InitWindow(screenWidth, screenHeight, "RetroDemo — Amiga 500 Style Demo");
Raylib.SetTargetFPS(60);
Raylib.InitAudioDevice();

// ── Background music ─────────────────────────────────────────────────────────
bool hasMusicLoaded = false;
Music bgMusic = default;

string[] musicSearchPaths =
[
    Path.Combine(AppContext.BaseDirectory, "Assets", "music.mp3"),
    Path.Combine(AppContext.BaseDirectory, "Assets", "music.ogg"),
    Path.Combine(AppContext.BaseDirectory, "Assets", "music.wav"),
    "Assets/music.mp3",
    "Assets/music.ogg",
    "music.mp3",
];

foreach (string path in musicSearchPaths)
{
    if (File.Exists(path))
    {
        bgMusic = Raylib.LoadMusicStream(path);
        Raylib.SetMusicVolume(bgMusic, 0.8f);
        Raylib.PlayMusicStream(bgMusic);
        hasMusicLoaded = true;
        break;
    }
}

// ── Scenes ───────────────────────────────────────────────────────────────────
IScene[] scenes =
[
    new TeleprompterScene(screenWidth, screenHeight),
    new FaceMorphScene(screenWidth, screenHeight),
    new SinusScene(screenWidth, screenHeight),
];

int sceneIndex = 0;

// ── Main loop ─────────────────────────────────────────────────────────────────
while (!Raylib.WindowShouldClose() && sceneIndex < scenes.Length)
{
    float dt = Raylib.GetFrameTime();

    // Update streaming music buffer every frame
    if (hasMusicLoaded)
        Raylib.UpdateMusicStream(bgMusic);

    // Space or Enter skips the current scene
    if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
        sceneIndex++;

    // Escape quits immediately
    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        break;

    if (sceneIndex >= scenes.Length)
        break;

    bool sceneDone = scenes[sceneIndex].Update(dt);

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);
    scenes[sceneIndex].Draw();
    Raylib.EndDrawing();

    if (sceneDone)
        sceneIndex++;
}

// ── Cleanup ───────────────────────────────────────────────────────────────────
foreach (IScene scene in scenes)
    scene.Dispose();

if (hasMusicLoaded)
{
    Raylib.StopMusicStream(bgMusic);
    Raylib.UnloadMusicStream(bgMusic);
}

Raylib.CloseAudioDevice();
Raylib.CloseWindow();
