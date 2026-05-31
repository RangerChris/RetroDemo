using System.Numerics;
using Raylib_cs;
using static RetroDemo.ColorHelper;

namespace RetroDemo.Scenes;

/// <summary>
/// Ten classic sine-based demo effects cycling in sequence, with a smooth
/// text scroller running along the bottom throughout.
/// </summary>
public sealed class SinusScene : IScene
{
    private readonly int _w;
    private readonly int _h;

    // ── effects ───────────────────────────────────────────────────────────────
    private const int  EffectCount    = 10;
    private const float EffectDuration = 5.0f;   // seconds per effect
    private const float FadeTime       = 0.6f;   // crossfade overlap

    private float _time       = 0f;
    private int   _effectIdx  = 0;
    private float _effectTime = 0f;

    // ── low-res plasma buffer (classic 320×200 Amiga resolution) ──────────────
    private const int LW = 320;
    private const int LH = 200;
    private RenderTexture2D _plasma;

    // ── starfield (shared across multiple effects) ────────────────────────────
    private readonly Star[] _stars;
    private struct Star { public float X, Y, Z; }
    private const int StarCount = 220;

    // ── bobs ──────────────────────────────────────────────────────────────────
    private const int BobCount = 12;
    private RenderTexture2D _bobRT;

    // ── lissajous trail ───────────────────────────────────────────────────────
    private const int TrailLen = 1200;
    private readonly Vector2[] _trail = new Vector2[TrailLen];
    private int   _trailHead  = 0;
    private float _lissT      = 0f;

    // ── tunnel ────────────────────────────────────────────────────────────────
    private RenderTexture2D _tunnelRT;

    // ── scroller ──────────────────────────────────────────────────────────────
    private const string ScrollText =
        "  *** RETRO DEMO — AMIGA 500 STYLE ***   " +
        "CODED IN C# AND .NET 10 WITH RAYLIB   " +
        "REPLACE THIS TEXT WITH YOUR OWN MESSAGE!   " +
        "GREETINGS TO ALL DEMO CODERS EVERYWHERE   " +
        "PRESS SPACE TO SKIP EFFECTS   " +
        "*** SINUS RULES ***   ";

    private float _scrollX;
    private const int ScrollFontSize = 42;
    private const int ScrollYBase    = 0;   // offset from bottom — computed on draw

    // ── camera for 3-D effects ────────────────────────────────────────────────
    private Camera3D _cam3d = new()
    {
        Position   = new Vector3(0, 12f, 0.1f),
        Target     = Vector3.Zero,
        Up         = Vector3.UnitZ,
        FovY       = 50f,
        Projection = CameraProjection.Perspective,
    };

    // ─────────────────────────────────────────────────────────────────────────
    public SinusScene(int w, int h)
    {
        _w = w;
        _h = h;

        _plasma   = Raylib.LoadRenderTexture(LW, LH);
        _bobRT    = Raylib.LoadRenderTexture(w, h);
        _tunnelRT = Raylib.LoadRenderTexture(LW, LH);

        // Initialise stars
        var rng = new Random(77);
        _stars = new Star[StarCount];
        for (int i = 0; i < StarCount; i++)
            _stars[i] = new Star
            {
                X = (rng.NextSingle() - 0.5f) * 2f,
                Y = (rng.NextSingle() - 0.5f) * 2f,
                Z = rng.NextSingle(),
            };

        // Scroll starts off-screen right
        _scrollX = w + 20f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public bool Update(float dt)
    {
        _time       += dt;
        _effectTime += dt;

        if (_effectTime >= EffectDuration)
        {
            _effectTime -= EffectDuration;
            _effectIdx++;
            if (_effectIdx >= EffectCount)
                return true;
        }

        // Advance lissajous position
        _lissT += dt * 0.9f;

        // Update scroll position
        _scrollX -= dt * 220f;

        // Measure total scroll text width so we can loop
        int totalW = Raylib.MeasureText(ScrollText, ScrollFontSize);
        if (_scrollX < -totalW)
            _scrollX += totalW + _w;

        // Advance stars
        for (int i = 0; i < StarCount; i++)
        {
            _stars[i].Z -= dt * 0.6f;
            if (_stars[i].Z <= 0f)
            {
                var rng = new Random(i + (int)(_time * 100));
                _stars[i] = new Star
                {
                    X = (rng.NextSingle() - 0.5f) * 2f,
                    Y = (rng.NextSingle() - 0.5f) * 2f,
                    Z = 1f,
                };
            }
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Draw()
    {
        float et = _effectTime;   // time within current effect
        float t  = _time;

        // Fade between effects
        float alpha = 1f;
        if (et < FadeTime)       alpha = et / FadeTime;
        else if (et > EffectDuration - FadeTime)
            alpha = (EffectDuration - et) / FadeTime;

        Raylib.ClearBackground(Color.Black);

        switch (_effectIdx)
        {
            case 0: DrawMultiWaves(t, alpha);       break;
            case 1: DrawPlasma(t, alpha);            break;
            case 2: DrawCopperBars(t, alpha);        break;
            case 3: DrawStarfield(t, alpha);         break;
            case 4: DrawBobs(t, alpha);              break;
            case 5: DrawSineLandscape(t, alpha);     break;
            case 6: DrawLissajous(t, alpha);         break;
            case 7: DrawTunnel(t, alpha);            break;
            case 8: DrawInterference(t, alpha);      break;
            case 9: DrawDotRotator(t, alpha);        break;
            default: Raylib.ClearBackground(Color.Black); break;
        }

        DrawScrollerBar();
        DrawScanlines();

        // Effect name
        DrawEffectLabel(_effectIdx, alpha);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 1 — Multi-layer Sine Waves
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawMultiWaves(float t, float alpha)
    {
        Raylib.ClearBackground(Rgba(5, 0, 20, 255));

        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;
        float cy    = drawH / 2f;

        // 6 waves with different params
        (float freq, float amp, float speed, float hue)[] waves =
        [
            (1.0f, 80f, 1.2f,   0f),
            (1.5f, 55f, 0.9f,  60f),
            (2.0f, 45f, 1.6f, 120f),
            (2.7f, 35f, 2.1f, 180f),
            (3.5f, 25f, 1.8f, 240f),
            (4.2f, 18f, 2.5f, 300f),
        ];

        foreach (var (freq, amp, speed, baseHue) in waves)
        {
            float hue = (baseHue + t * 40f) % 360f;
            byte  a   = (byte)(alpha * 220);
            var   c   = Raylib.ColorFromHSV(hue, 1f, 1f);
            c = Rgba(c.R, c.G, c.B, a);

            for (int x = 0; x < _w - 1; x++)
            {
                float y1 = cy + amp * MathF.Sin(freq * x * 0.015f + t * speed);
                float y2 = cy + amp * MathF.Sin(freq * (x + 1) * 0.015f + t * speed);
                Raylib.DrawLineEx(new Vector2(x, y1), new Vector2(x + 1, y2), 2.5f, c);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 2 — Classic Plasma
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawPlasma(float t, float alpha)
    {
        // Render to low-res texture, then scale up for that pixel-art retro look
        Raylib.BeginTextureMode(_plasma);
        Raylib.ClearBackground(Color.Black);

        for (int py = 0; py < LH; py++)
        for (int px = 0; px < LW; px++)
        {
            float v = MathF.Sin(px * 0.06f + t * 1.5f)
                    + MathF.Sin(py * 0.05f + t * 1.2f)
                    + MathF.Sin((px + py) * 0.04f + t)
                    + MathF.Sin(MathF.Sqrt(px * px + py * py) * 0.07f - t * 2f);
            float hue = ((v + 4f) / 8f * 360f + t * 60f) % 360f;
            Raylib.DrawPixel(px, py, Raylib.ColorFromHSV(hue, 1f, 1f));
        }

        Raylib.EndTextureMode();

        // Scale to full window (Y flip for render texture)
        int scrollH = ScrollFontSize + 20;
        byte pa = (byte)(alpha * 255);
        Raylib.DrawTexturePro(_plasma.Texture,
            new Rectangle(0, 0, LW, -LH),
            new Rectangle(0, 0, _w, _h - scrollH),
            Vector2.Zero, 0f, Rgba(255, 255, 255, pa));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 3 — Copper Bars
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawCopperBars(float t, float alpha)
    {
        Raylib.ClearBackground(Color.Black);

        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;

        // 8 copper-bar groups
        for (int b = 0; b < 8; b++)
        {
            float sinPhase = t * (1.0f + b * 0.15f) + b * MathF.PI * 0.4f;
            int barCenter  = (int)(drawH / 2f + (drawH * 0.38f) * MathF.Sin(sinPhase));
            int barHeight  = 40 + (int)(20 * MathF.Abs(MathF.Sin(t * 0.7f + b)));
            float baseHue  = (b * 45f + t * 50f) % 360f;

            for (int row = 0; row < barHeight; row++)
            {
                int y = barCenter - barHeight / 2 + row;
                if (y < 0 || y >= drawH) continue;
                float f   = row / (float)barHeight;
                float hue = (baseHue + f * 90f) % 360f;
                float bri = 0.4f + 0.6f * MathF.Sin(f * MathF.PI);
                var c = Raylib.ColorFromHSV(hue, 1f, bri);
                byte ca = (byte)(alpha * 255);
                Raylib.DrawRectangle(0, y, _w, 1, Rgba(c.R, c.G, c.B, ca));
            }
        }

        // Centered label
        string lbl = "COPPER BARS";
        int lfs = 24;
        int lw  = Raylib.MeasureText(lbl, lfs);
        byte la = (byte)(alpha * 180);
        Raylib.DrawText(lbl, (_w - lw) / 2, drawH / 2 - lfs / 2, lfs,
                        Rgba(255, 255, 255, la));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 4 — Starfield (perspective with sine wobble)
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawStarfield(float t, float alpha)
    {
        Raylib.ClearBackground(Color.Black);
        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;
        float cx = _w / 2f, cy = drawH / 2f;

        foreach (var s in _stars)
        {
            if (s.Z <= 0f) continue;
            float sx = (s.X / s.Z) * _w * 0.5f + cx
                        + 12f * MathF.Sin(t * 0.8f + s.Y * 5f);
            float sy = (s.Y / s.Z) * drawH * 0.5f + cy;
            if (sx < 0 || sx >= _w || sy < 0 || sy >= drawH) continue;

            float brightness = 1f - s.Z;
            float r2 = Math.Max(1f, (1f - s.Z) * 3.5f);
            byte  sb = (byte)(brightness * alpha * 255);
            // Colour based on depth
            float hue = (s.Z * 200f + 180f) % 360f;
            var sc = Raylib.ColorFromHSV(hue, 0.3f + brightness * 0.7f, 1f);
            Raylib.DrawCircleV(new Vector2(sx, sy), r2, Rgba(sc.R, sc.G, sc.B, sb));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 5 — Bouncing Bobs (additive blending)
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawBobs(float t, float alpha)
    {
        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;

        // Render bobs to their own RT with additive blend for glow
        Raylib.BeginTextureMode(_bobRT);
        Raylib.ClearBackground(Rgba(5, 0, 18, 255));

        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int b = 0; b < BobCount; b++)
        {
            float phase = b * MathF.Tau / BobCount;
            float ax    = 0.85f + 0.1f * MathF.Sin(t * 0.3f + phase);
            float ay    = 0.75f + 0.1f * MathF.Cos(t * 0.4f + phase);
            float bx = _w / 2f + ax * (_w * 0.4f)
                         * MathF.Sin(t * (0.8f + b * 0.07f) + phase);
            float by = drawH / 2f + ay * (drawH * 0.35f)
                         * MathF.Cos(t * (0.7f + b * 0.09f) + phase * 1.3f);

            float hue = (b * 30f + t * 60f) % 360f;
            var c = Raylib.ColorFromHSV(hue, 1f, 1f);

            // Soft outer glow
            for (int g = 5; g >= 1; g--)
            {
                byte ga = (byte)(30 * g);
                Raylib.DrawCircleV(new Vector2(bx, by),
                                   18f + g * 7f, Rgba(c.R, c.G, c.B, ga));
            }
            // Core
            Raylib.DrawCircleV(new Vector2(bx, by), 18f, c);
        }
        Raylib.EndBlendMode();
        Raylib.EndTextureMode();

        byte ta = (byte)(alpha * 255);
        Raylib.DrawTexturePro(_bobRT.Texture,
            new Rectangle(0, 0, _w, -_h),
            new Rectangle(0, 0, _w, drawH),
            Vector2.Zero, 0f, Rgba(255, 255, 255, ta));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 6 — 3-D Sine Landscape (wireframe grid)
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawSineLandscape(float t, float alpha)
    {
        Raylib.ClearBackground(Rgba(0, 5, 20, 255));

        int cols = 40, rows = 30;
        float cellW = 2.2f, cellD = 2.2f;
        float originX = -cols / 2f * cellW;
        float originZ = -rows / 2f * cellD + t * 4f;  // camera scroll

        _cam3d.Position = new Vector3(0, 14f, 5f);
        _cam3d.Target   = new Vector3(0, 0, 0);
        _cam3d.Up       = Vector3.UnitY;
        _cam3d.FovY     = 50f;

        // Scissor to leave scroller area
        // Draw full then overdraw scroller area
        Raylib.BeginMode3D(_cam3d);

        // Draw grid rows
        for (int rr = 0; rr < rows; rr++)
        for (int cc = 0; cc < cols; cc++)
        {
            float x0 = originX + cc * cellW;
            float z0 = originZ + rr * cellD;
            float x1 = x0 + cellW;
            float z1 = z0 + cellD;

            float GetHeight(float x, float z)
            {
                return 1.5f * MathF.Sin(x * 0.4f + t * 1.2f)
                     + 1.0f * MathF.Cos(z * 0.3f + t * 0.9f)
                     + 0.8f * MathF.Sin((x + z) * 0.25f + t * 1.5f);
            }

            float h00 = GetHeight(x0, z0);
            float h10 = GetHeight(x1, z0);
            float h01 = GetHeight(x0, z1);

            float dist = MathF.Sqrt(x0 * x0 + z0 * z0) * 0.05f;
            float hue  = ((h00 + 3f) / 6f * 240f + 180f + t * 30f) % 360f;
            float bri  = Math.Clamp(1f - dist * 0.15f, 0.2f, 1f);
            var c = Raylib.ColorFromHSV(hue, 1f, bri);
            byte ca = (byte)(alpha * 255);
            c = Rgba(c.R, c.G, c.B, ca);

            var p00 = new Vector3(x0, h00, z0);
            var p10 = new Vector3(x1, h10, z0);
            var p01 = new Vector3(x0, h01, z1);

            Raylib.DrawLine3D(p00, p10, c);
            Raylib.DrawLine3D(p00, p01, c);
        }

        Raylib.EndMode3D();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 7 — Lissajous Curves (with fading trail)
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawLissajous(float t, float alpha)
    {
        Raylib.ClearBackground(Rgba(0, 0, 20, 255));

        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;
        float cx = _w / 2f, cy = drawH / 2f;
        float rx = _w * 0.42f, ry = drawH * 0.42f;

        // Ratio changes over time for variety
        float a = 3f, b = 2f;
        float delta = _lissT * 0.4f;

        // Add current point to trail
        float lx = cx + rx * MathF.Sin(a * _lissT + delta);
        float ly = cy + ry * MathF.Sin(b * _lissT);
        _trail[_trailHead] = new Vector2(lx, ly);
        _trailHead = (_trailHead + 1) % TrailLen;

        // Draw trail with fading colour
        for (int i = 0; i < TrailLen - 1; i++)
        {
            int  idx0 = (_trailHead + i)       % TrailLen;
            int  idx1 = (_trailHead + i + 1)   % TrailLen;
            if (_trail[idx0] == Vector2.Zero) continue;

            float ageF = i / (float)TrailLen;
            float hue  = (ageF * 360f + t * 80f) % 360f;
            byte  ba   = (byte)(ageF * alpha * 180f);
            var   col  = Raylib.ColorFromHSV(hue, 1f, 1f);
            Raylib.DrawLineEx(_trail[idx0], _trail[idx1], 1.5f,
                              Rgba(col.R, col.G, col.B, ba));
        }

        // Bright head dot
        byte ha = (byte)(alpha * 255);
        Raylib.DrawCircleV(new Vector2(lx, ly), 5f, Rgba(255, 255, 255, ha));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 8 — Tunnel Effect
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawTunnel(float t, float alpha)
    {
        // Render pixel-by-pixel into the low-res tunnel texture
        Raylib.BeginTextureMode(_tunnelRT);
        Raylib.ClearBackground(Color.Black);

        for (int py = 0; py < LH; py++)
        for (int px = 0; px < LW; px++)
        {
            float fx = (px / (float)LW - 0.5f) * 2f;
            float fy = (py / (float)LH - 0.5f) * 2f;

            // Add sine wobble to the tunnel centre
            float cx2 = 0.2f * MathF.Sin(t * 0.7f);
            float cy2 = 0.2f * MathF.Cos(t * 0.5f);
            float dx = fx - cx2, dy = fy - cy2;

            float dist = MathF.Sqrt(dx * dx + dy * dy) + 0.001f;
            float angle = MathF.Atan2(dy, dx);

            float u = (angle / MathF.PI + t * 0.5f) % 1f;
            float v = (1f / dist + t) % 1f;

            float hue = ((u + v * 0.3f) * 360f) % 360f;
            float bri = Math.Clamp(1f - dist * 0.3f, 0.1f, 1f);
            Raylib.DrawPixel(px, py, Raylib.ColorFromHSV(hue, 1f, bri));
        }

        Raylib.EndTextureMode();

        int scrollH = ScrollFontSize + 20;
        byte ta = (byte)(alpha * 255);
        Raylib.DrawTexturePro(_tunnelRT.Texture,
            new Rectangle(0, 0, LW, -LH),
            new Rectangle(0, 0, _w, _h - scrollH),
            Vector2.Zero, 0f, Rgba(255, 255, 255, ta));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 9 — Interference / Moiré Rings
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawInterference(float t, float alpha)
    {
        Raylib.ClearBackground(Color.Black);

        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;

        // Two moving ring centres
        float cx1 = _w * (0.35f + 0.12f * MathF.Sin(t * 0.6f));
        float cy1 = drawH * (0.5f  + 0.15f * MathF.Cos(t * 0.7f));
        float cx2 = _w * (0.65f + 0.12f * MathF.Cos(t * 0.8f));
        float cy2 = drawH * (0.5f  - 0.15f * MathF.Sin(t * 0.5f));

        int maxR = (int)(MathF.Sqrt(_w * _w + drawH * drawH) / 2) + 40;

        for (int r = 4; r < maxR; r += 8)
        {
            float t1   = r / 30f - t * 2.5f;
            float t2   = r / 25f - t * 2.0f;
            float wave = 0.5f + 0.5f * (MathF.Sin(t1) * MathF.Sin(t2));
            if (wave < 0.5f) continue;  // Only draw bright fringes

            float hue = ((r * 1.5f + t * 60f)) % 360f;
            byte  ba  = (byte)(wave * alpha * 200f);
            var   c   = Raylib.ColorFromHSV(hue, 1f, 1f);

            Raylib.DrawRing(new Vector2(cx1, cy1), r - 2, r + 2,
                            0, 360, 60, Rgba(c.R, c.G, c.B, ba));
            Raylib.DrawRing(new Vector2(cx2, cy2), r - 2, r + 2,
                            0, 360, 60, Rgba(c.B, c.R, c.G, ba));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Effect 10 — Dot Rotator / Torus Points
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawDotRotator(float t, float alpha)
    {
        Raylib.ClearBackground(Rgba(0, 0, 15, 255));

        int scrollH = ScrollFontSize + 20;
        int drawH   = _h - scrollH;
        float cx = _w / 2f, cy = drawH / 2f;

        int torusU = 28, torusV = 18;
        float R = 200f, r2 = 85f;   // major/minor radii in px

        float rotX = t * 0.4f;
        float rotY = t * 0.7f;

        for (int u = 0; u < torusU; u++)
        for (int v = 0; v < torusV; v++)
        {
            float au = u * MathF.Tau / torusU;
            float av = v * MathF.Tau / torusV;

            // Sine displacement ripple
            float disp = 12f * MathF.Sin(au * 3f + t * 2.5f)
                               * MathF.Cos(av * 2f + t * 1.8f);

            float x3 = (R + r2 * MathF.Cos(av) + disp) * MathF.Cos(au);
            float y3 = (R + r2 * MathF.Cos(av) + disp) * MathF.Sin(au);
            float z3 = r2 * MathF.Sin(av);

            // Rotate X
            float y4 = y3 * MathF.Cos(rotX) - z3 * MathF.Sin(rotX);
            float z4 = y3 * MathF.Sin(rotX) + z3 * MathF.Cos(rotX);

            // Rotate Y
            float x5 = x3 * MathF.Cos(rotY) + z4 * MathF.Sin(rotY);
            float z5 = -x3 * MathF.Sin(rotY) + z4 * MathF.Cos(rotY);

            float depth = 600f;
            float pz    = z5 + depth;
            if (pz <= 0.01f) continue;

            float px = cx + x5 * depth / pz;
            float py = cy + y4 * depth / pz;

            float bright = Math.Clamp((z5 + r2 + R) / (2 * (R + r2)), 0.2f, 1f);
            float hue    = (au * 180f / MathF.PI + t * 50f + v * 15f) % 360f;
            var   c      = Raylib.ColorFromHSV(hue, 1f, bright);
            byte  ba     = (byte)(alpha * 255 * bright);
            float dotR   = Math.Max(1.5f, (depth / pz) * 3.5f);

            Raylib.DrawCircleV(new Vector2(px, py), dotR, Rgba(c.R, c.G, c.B, ba));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Scroller Bar (runs at the bottom across ALL effects)
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawScrollerBar()
    {
        int scrollH = ScrollFontSize + 20;
        int barY    = _h - scrollH;

        // Background bar with gradient
        for (int row = 0; row < scrollH; row++)
        {
            float f = row / (float)scrollH;
            float hue = (_time * 60f + f * 40f) % 360f;
            float bri = 0.15f + 0.1f * f;
            var c = Raylib.ColorFromHSV(hue, 0.9f, bri);
            Raylib.DrawRectangle(0, barY + row, _w, 1, c);
        }

        // Separator line
        Raylib.DrawRectangle(0, barY, _w, 2,
                             Rgba(200, 255, 200, 200));

        // Sine-modulated Y for the text
        float textSineY = 4f * MathF.Sin(_time * 3f);
        int textY = barY + (scrollH - ScrollFontSize) / 2 + (int)textSineY;

        // We draw the scroller text twice so it wraps seamlessly
        int totalW = Raylib.MeasureText(ScrollText, ScrollFontSize);
        int sx = (int)_scrollX;

        // Clip scroller to the bar area
        Raylib.BeginScissorMode(0, barY, _w, scrollH);

        // Rainbow colour on each character
        DrawRainbowText(ScrollText, sx, textY, ScrollFontSize);
        DrawRainbowText(ScrollText, sx + totalW, textY, ScrollFontSize);

        Raylib.EndScissorMode();
    }

    private void DrawRainbowText(string text, int x, int y, int fontSize)
    {
        float charWidth = fontSize * 0.55f;
        for (int ci = 0; ci < text.Length; ci++)
        {
            int cx2 = x + (int)(ci * charWidth);
            if (cx2 + charWidth < 0 || cx2 > _w) continue;

            float hue = ((ci * 12f) + _time * 120f) % 360f;
            var c = Raylib.ColorFromHSV(hue, 1f, 1f);
            Raylib.DrawText(text[ci].ToString(), cx2, y, fontSize, c);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static readonly string[] EffectNames =
    [
        "1. MULTI-LAYER SINE WAVES",
        "2. CLASSIC PLASMA",
        "3. COPPER BARS",
        "4. STARFIELD",
        "5. BOUNCING BOBS",
        "6. 3D SINE LANDSCAPE",
        "7. LISSAJOUS CURVES",
        "8. TUNNEL EFFECT",
        "9. INTERFERENCE RINGS",
        "10. DOT ROTATOR / TORUS",
    ];

    private void DrawEffectLabel(int idx, float alpha)
    {
        if (idx >= EffectNames.Length) return;
        int   fs = 18;
        byte  ba = (byte)(Math.Clamp(alpha * 1.5f, 0f, 1f) * 160);
        Raylib.DrawText(EffectNames[idx], 12, 10, fs, Rgba(255, 255, 200, ba));
    }

    private void DrawScanlines()
    {
        var sc = Rgba(0, 0, 0, 50);
        for (int y = 0; y < _h; y += 2)
            Raylib.DrawRectangle(0, y, _w, 1, sc);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Dispose()
    {
        Raylib.UnloadRenderTexture(_plasma);
        Raylib.UnloadRenderTexture(_bobRT);
        Raylib.UnloadRenderTexture(_tunnelRT);
    }
}
