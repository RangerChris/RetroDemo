using System.Diagnostics;
using NAudio.Wave;
using RetroDemo;
using RetroDemo.Scenes;

ApplicationConfiguration.Initialize();

const int windowWidth = 1280;
const int windowHeight = 720;

using var window = new Form();
window.Text = "RetroDemo — DirectX 12";
window.FormBorderStyle = FormBorderStyle.FixedSingle;
window.ClientSize = new Size(windowWidth, windowHeight);
window.MaximizeBox = false;
window.MinimizeBox = true;
window.TopMost = false;
window.StartPosition = FormStartPosition.CenterScreen;
window.KeyPreview = true;

var shouldQuit = false;
var skipScene = false;
window.KeyDown += (_, e) =>
{
    if (e.KeyCode == Keys.Escape)
    {
        shouldQuit = true;
        return;
    }

    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
    {
        skipScene = true;
    }
};

window.Show();

using var renderer = new Dx12Renderer(window.Handle, windowWidth, windowHeight);

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

foreach (var path in musicSearchPaths)
{
    if (!File.Exists(path))
    {
        continue;
    }

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
    new TeleprompterScene(windowWidth, windowHeight),
    new SinusScene(windowWidth, windowHeight),
];
var sceneIndex = 0;

var stopwatch = Stopwatch.StartNew();
var lastTime = stopwatch.Elapsed.TotalSeconds;

// ── Main loop ────────────────────────────────────────────────────────────────
while (!shouldQuit && !window.IsDisposed)
{
    Application.DoEvents();

    var now = stopwatch.Elapsed.TotalSeconds;
    var dt = (float)(now - lastTime);
    lastTime = now;

    var activeScene = scenes[sceneIndex];
    var sceneDone = activeScene.Update(dt);
    if (skipScene)
    {
        sceneDone = true;
        skipScene = false;
    }

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
            scenes[sceneIndex] = new SinusScene(windowWidth, windowHeight);
            activeScene = scenes[sceneIndex];
        }
    }

    activeScene.Draw(renderer);
}

foreach (var scene in scenes)
{
    scene.Dispose();
}

musicOut?.Stop();
musicOut?.Dispose();
musicReader?.Dispose();
