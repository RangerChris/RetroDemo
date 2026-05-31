using System.Numerics;
using Raylib_cs;
using static RetroDemo.ColorHelper;

namespace RetroDemo.Scenes;

/// <summary>
/// Classic Amiga teleprompter scene with copper-bar header, typewriter text and
/// a full-screen scanline overlay for that authentic retro feel.
/// </summary>
public sealed class TeleprompterScene : IScene
{
    // ── dimensions ────────────────────────────────────────────────────────────
    private readonly int _w;
    private readonly int _h;

    // ── messages ──────────────────────────────────────────────────────────────
    private static readonly string[] Messages =
    [
        "WELCOME TO RETRO DEMO",
        "CREATED BY ONE OR MORE MACHINES",
    ];

    // ── timing ────────────────────────────────────────────────────────────────
    private const float TypeSpeed    = 18f;   // chars per second
    private const float HoldDuration = 2.4f;  // seconds to hold full message
    private const float FadeDuration = 0.8f;  // seconds to fade in/out

    private enum Phase { FadeIn, Typing, Hold, FadeOut, Done }

    private Phase  _phase     = Phase.FadeIn;
    private int    _msgIndex  = 0;
    private float  _phaseTime = 0f;
    private float  _totalTime = 0f;
    private float  _alpha     = 0f;
    private int    _visChars  = 0;

    // ── copper bars ───────────────────────────────────────────────────────────
    private const int CopperHeight = 48;

    public TeleprompterScene(int screenWidth, int screenHeight)
    {
        _w = screenWidth;
        _h = screenHeight;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public bool Update(float dt)
    {
        _totalTime += dt;
        _phaseTime += dt;

        switch (_phase)
        {
            case Phase.FadeIn:
                _alpha = Math.Min(_phaseTime / FadeDuration, 1f);
                if (_phaseTime >= FadeDuration)
                {
                    _phase     = Phase.Typing;
                    _phaseTime = 0f;
                    _visChars  = 0;
                }
                break;

            case Phase.Typing:
                _alpha     = 1f;
                _visChars  = (int)(_phaseTime * TypeSpeed);
                int maxChars = Messages[_msgIndex].Length;
                if (_visChars >= maxChars)
                {
                    _visChars  = maxChars;
                    _phase     = Phase.Hold;
                    _phaseTime = 0f;
                }
                break;

            case Phase.Hold:
                _alpha = 1f;
                if (_phaseTime >= HoldDuration)
                {
                    _phase     = Phase.FadeOut;
                    _phaseTime = 0f;
                }
                break;

            case Phase.FadeOut:
                _alpha = 1f - Math.Min(_phaseTime / FadeDuration, 1f);
                if (_phaseTime >= FadeDuration)
                {
                    _msgIndex++;
                    if (_msgIndex >= Messages.Length)
                    {
                        _phase = Phase.Done;
                    }
                    else
                    {
                        _phase     = Phase.FadeIn;
                        _phaseTime = 0f;
                        _visChars  = 0;
                    }
                }
                break;

            case Phase.Done:
                return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Draw()
    {
        // ── background ────────────────────────────────────────────────────────
        Raylib.ClearBackground(Color.Black);

        // ── copper-bar header & footer ────────────────────────────────────────
        DrawCopperBars(0, CopperHeight);
        DrawCopperBars(_h - CopperHeight, CopperHeight);

        // ── centre message ────────────────────────────────────────────────────
        if (_phase != Phase.Done && _alpha > 0f)
        {
            string msg   = Messages[_msgIndex];
            string shown = msg[..Math.Clamp(_visChars, 0, msg.Length)];

            int fontSize = _w / 24;  // Scales with window width
            int textW    = Raylib.MeasureText(shown, fontSize);
            int textX    = (_w - textW) / 2;
            int textY    = (_h - fontSize) / 2;

            // Glow shadow
            byte glowAlpha = (byte)(_alpha * 80);
            for (int d = 4; d >= 1; d--)
            {
                var glow = Rgba(255, 160, 0, glowAlpha);
                Raylib.DrawText(shown, textX - d, textY + d, fontSize, glow);
                Raylib.DrawText(shown, textX + d, textY + d, fontSize, glow);
            }

            // Main text – warm amber/gold, classic Amiga colour
            byte mainAlpha = (byte)(_alpha * 255);
            var mainColor  = Rgba(255, 210, 60, mainAlpha);
            Raylib.DrawText(shown, textX, textY, fontSize, mainColor);

            // Cursor blink while typing
            if (_phase == Phase.Typing)
            {
                bool blink = (int)(_totalTime * 2f) % 2 == 0;
                if (blink)
                {
                    int cursorX = textX + textW + 4;
                    Raylib.DrawRectangle(cursorX, textY, fontSize / 2, fontSize, mainColor);
                }
            }

            // Sub-title decoration lines
            int lineY1 = textY - 20;
            int lineY2 = textY + fontSize + 14;
            byte lineA  = (byte)(_alpha * 200);
            var  lineC  = Rgba(255, 160, 0, lineA);
            Raylib.DrawLine(textX, lineY1, textX + textW, lineY1, lineC);
            Raylib.DrawLine(textX, lineY2, textX + textW, lineY2, lineC);
        }

        // ── scanline overlay ──────────────────────────────────────────────────
        DrawScanlines();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void DrawCopperBars(int startY, int height)
    {
        for (int row = 0; row < height; row++)
        {
            // Each row gets a hue derived from position + time
            float t    = _totalTime;
            float hue  = ((row * 360f / height) + t * 80f) % 360f;
            float brightness = 0.55f + 0.45f * MathF.Sin(row * MathF.PI / height);
            var   color = Raylib.ColorFromHSV(hue, 1f, brightness);
            Raylib.DrawRectangle(0, startY + row, _w, 1, color);
        }

        // Highlight edge line
        var white = Rgba(255, 255, 255, 120);
        Raylib.DrawRectangle(0, startY,              _w, 2, white);
        Raylib.DrawRectangle(0, startY + height - 2, _w, 2, white);
    }

    private void DrawScanlines()
    {
        var scanColor = Rgba(0, 0, 0, 80);
        for (int y = 0; y < _h; y += 2)
            Raylib.DrawRectangle(0, y, _w, 1, scanColor);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Dispose() { /* nothing to unload */ }
}
