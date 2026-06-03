# RetroDemo — Amiga 500 Style Demo

A classic Amiga 500 demo written in **.NET 10** using [Raylib-cs](https://github.com/raylib-cs/raylib-cs) (Raylib 6.0 bindings).

## Scenes

| # | Scene | Description |
|---|-------|-------------|
| 1 | **Teleprompter** | Typewriter-style messages with animated copper-bar header/footer and scanline overlay |
| 2 | **Sinus Effects** | 10 classic sine-based demo effects cycling every 5 seconds, with a rainbow text scroller running at the bottom throughout |

### The 10 sinus effects

1. Multi-layer sine waves
2. Classic plasma (320×200 retro resolution, pixel-perfect upscale)
3. Copper bars
4. Star field (perspective, sine wobble)
5. Bouncing bobs (additive-blended glow)
6. 3-D sine landscape (wireframe)
7. Lissajous curves (fading trail)
8. Tunnel effect (fisheye tunnel with sine wobble)
9. Interference rings (moiré)
10. Dot rotator / torus (sine-displaced point cloud)

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A modern GPU (Raylib uses OpenGL 3.3)
- **Optional:** an MP3/OGG/WAV music file (see [Assets/README.md](RetroDemo/Assets/README.md))

## Running

```sh
cd RetroDemo
dotnet run
```

Press **Space / Enter** to skip to the next scene, **Escape** to quit.

## Adding background music

Copy your audio file to `RetroDemo/Assets/` and name it `music.mp3`, `music.ogg`
or `music.wav`.  The demo picks it up automatically on the next run.  See
[RetroDemo/Assets/README.md](RetroDemo/Assets/README.md) for details.

## Project structure

```
RetroDemo/
├── RetroDemo.csproj          # .NET 10 project, Raylib-cs 8.0 dependency
├── Program.cs                # Window, audio, scene loop
├── ColorHelper.cs            # Rgba() helper (resolves Raylib-cs overload ambiguity)
├── Scenes/
│   ├── IScene.cs             # Scene interface
│   ├── TeleprompterScene.cs  # Scene 1
│   └── SinusScene.cs         # Scene 2
└── Assets/
    └── README.md             # Music file instructions
```
