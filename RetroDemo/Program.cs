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
    new SinusMultiWavesScene(windowWidth, windowHeight),
    new SinusPlasmaScene(windowWidth, windowHeight),
    new SinusCopperBarsScene(windowWidth, windowHeight),
    new SinusStarfieldScene(windowWidth, windowHeight),
    new SinusBobsScene(windowWidth, windowHeight),
    new SinusLandscapeScene(windowWidth, windowHeight),
    new SinusLissajousScene(windowWidth, windowHeight),
    new SinusTunnelScene(windowWidth, windowHeight),
    new SinusInterferenceScene(windowWidth, windowHeight),
    new SinusDotRotatorScene(windowWidth, windowHeight),
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
        if (sceneIndex < scenes.Length - 1)
        {
            sceneIndex++;
            activeScene = scenes[sceneIndex];
        }
        else
        {
            for (var i = 1; i < scenes.Length; i++)
            {
                scenes[i].Dispose();
            }

            scenes[1] = new SinusMultiWavesScene(windowWidth, windowHeight);
            scenes[2] = new SinusPlasmaScene(windowWidth, windowHeight);
            scenes[3] = new SinusCopperBarsScene(windowWidth, windowHeight);
            scenes[4] = new SinusStarfieldScene(windowWidth, windowHeight);
            scenes[5] = new SinusBobsScene(windowWidth, windowHeight);
            scenes[6] = new SinusLandscapeScene(windowWidth, windowHeight);
            scenes[7] = new SinusLissajousScene(windowWidth, windowHeight);
            scenes[8] = new SinusTunnelScene(windowWidth, windowHeight);
            scenes[9] = new SinusInterferenceScene(windowWidth, windowHeight);
            scenes[10] = new SinusDotRotatorScene(windowWidth, windowHeight);

            sceneIndex = 1;
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
