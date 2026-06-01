using System.Diagnostics;
using System.Numerics;
using System.Windows.Forms;
using NAudio.Wave;
using RetroDemo;
using RetroDemo.Scenes;

ApplicationConfiguration.Initialize();

const int WindowWidth = 1280;
const int WindowHeight = 720;

using var window = new Form
{
    Text = "RetroDemo — DirectX 12",
    FormBorderStyle = FormBorderStyle.FixedSingle,
    ClientSize = new Size(WindowWidth, WindowHeight),
    MaximizeBox = false,
    MinimizeBox = true,
    TopMost = false,
    StartPosition = FormStartPosition.CenterScreen,
    KeyPreview = true,
};

bool shouldQuit = false;
window.KeyDown += (_, e) =>
{
    if (e.KeyCode == Keys.Escape)
        shouldQuit = true;
};

window.Show();

using var renderer = new Dx12Renderer(window.Handle, WindowWidth, WindowHeight);

// ── Background music (optional) ─────────────────────────────────────────────
WaveOutEvent? musicOut = null;
AudioFileReader? musicReader = null;

string[] musicSearchPaths =
[
    Path.Combine(AppContext.BaseDirectory, "Assets", "music.mp3"),
    Path.Combine(AppContext.BaseDirectory, "Assets", "music.wav"),
    Path.Combine(AppContext.BaseDirectory, "music.mp3"),
    Path.Combine(AppContext.BaseDirectory, "music.wav"),
    "Assets/music.mp3",
    "Assets/music.wav",
    "music.mp3",
    "music.wav",
];

foreach (string path in musicSearchPaths)
{
    if (!File.Exists(path))
        continue;

    try
    {
        musicReader = new AudioFileReader(path) { Volume = 0.8f };
        musicOut = new WaveOutEvent();
        musicOut.Init(musicReader);
        musicOut.Play();
        break;
    }
    catch
    {
        musicOut?.Dispose();
        musicReader?.Dispose();
        musicOut = null;
        musicReader = null;
    }
}

// ── Scenes ───────────────────────────────────────────────────────────────────
IScene[] scenes =
[
    new TeleprompterScene(WindowWidth, WindowHeight),
    new FaceMorphScene(WindowWidth, WindowHeight),
    new SinusScene(WindowWidth, WindowHeight),
];
int sceneIndex = 0;

var stopwatch = Stopwatch.StartNew();
double lastTime = stopwatch.Elapsed.TotalSeconds;

// ── Main loop ────────────────────────────────────────────────────────────────
while (!shouldQuit && !window.IsDisposed)
{
    Application.DoEvents();

    double now = stopwatch.Elapsed.TotalSeconds;
    float dt = (float)(now - lastTime);
    lastTime = now;

    IScene activeScene = scenes[sceneIndex];
    bool sceneDone = activeScene.Update(dt);

    if (sceneDone)
    {
        // Loop the final sinus scene forever; earlier scenes advance once.
        if (sceneIndex < scenes.Length - 1)
        {
            sceneIndex++;
            activeScene = scenes[sceneIndex];
        }
        else
        {
            activeScene.Dispose();
            scenes[sceneIndex] = new SinusScene(WindowWidth, WindowHeight);
            activeScene = scenes[sceneIndex];
        }
    }

    activeScene.Draw(renderer);
}

foreach (IScene scene in scenes)
    scene.Dispose();

musicOut?.Stop();
musicOut?.Dispose();
musicReader?.Dispose();
