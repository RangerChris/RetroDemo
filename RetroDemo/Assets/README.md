# Assets

Place your background music file here so the demo can pick it up automatically.

## Supported formats

| File name   | Format                                   |
|-------------|------------------------------------------|
| `music.mp3` | MPEG Layer-3 audio (**recommended**)     |
| `music.ogg` | Ogg Vorbis                               |
| `music.wav` | Uncompressed PCM                         |

The demo scans for files in the order listed above and plays the first one it
finds.  If no file is present the demo runs silently.

## Quick start

1. Copy your audio file into this folder and rename it `music.mp3` (or
   `music.ogg` / `music.wav`).
2. Rebuild & run:

```sh
dotnet run --project RetroDemo
```

> **Tip** – Classic Amiga MOD/XM tracker music (converted to MP3 or OGG)
> works brilliantly with the retro aesthetic of this demo.
