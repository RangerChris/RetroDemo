using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusScene : IScene
{
    private const int EffectCount = 10;
    private const float EffectDuration = 5.0f;

    private const int StarCount = 180;
    private const int TrailLength = 220;

    private readonly Star[] _stars = new Star[StarCount];
    private readonly Vector2[] _trail = new Vector2[TrailLength];
    private readonly Random _rng = new(77);
    private readonly float[] _labelWidths = new float[EffectCount];

    private float _time;
    private int _effectIndex;
    private float _effectTime;
    private int _trailHead;

    private struct Star
    {
        public float X;
        public float Y;
        public float Z;
    }

    public SinusScene(int w, int h)
    {
        _ = w;
        _ = h;

        for (var i = 0; i < StarCount; i++)
        {
            _stars[i] = new Star
            {
                X = (_rng.NextSingle() - 0.5f) * 2f,
                Y = (_rng.NextSingle() - 0.5f) * 2f,
                Z = _rng.NextSingle(),
            };
        }
    }

    public bool Update(float deltaTime)
    {
        _time += deltaTime;
        _effectTime += deltaTime;

        if (_effectTime >= EffectDuration)
        {
            _effectTime -= EffectDuration;
            _effectIndex++;
            if (_effectIndex >= EffectCount)
            {
                return true;
            }
        }

        for (var i = 0; i < StarCount; i++)
        {
            _stars[i].Z -= deltaTime * 0.55f;
            if (_stars[i].Z <= 0f)
            {
                _stars[i].X = (_rng.NextSingle() - 0.5f) * 2f;
                _stars[i].Y = (_rng.NextSingle() - 0.5f) * 2f;
                _stars[i].Z = 1f;
            }
        }

        return false;
    }

    public void Draw(Dx12Renderer renderer)
    {
        var w = renderer.Width;
        var h = renderer.Height;
        var drawH = h - 86;
        var resolutionScale = MathF.Max(1f, MathF.Max(w / 1920f, drawH / 1080f));
        var t = _time;

        var fade = GetEffectFade();
        var bg = GetEffectBackground();

        renderer.Clear(bg);
        renderer.BeginOverlay();

        DrawEffect(renderer, w, drawH, t, fade, resolutionScale);

        DrawScroller(renderer, w, h, t);
        DrawLabel(renderer, w);
        DrawScanlines(renderer, w, h, resolutionScale);

        renderer.EndOverlay();
    }

    private float GetEffectFade()
    {
        const float fadeTime = 0.6f;
        if (_effectTime < fadeTime)
        {
            return _effectTime / fadeTime;
        }

        if (_effectTime > EffectDuration - fadeTime)
        {
            return (EffectDuration - _effectTime) / fadeTime;
        }

        return 1f;
    }

    private Vector4 GetEffectBackground()
    {
        return _effectIndex switch
        {
            0 => new Vector4(0.03f, 0.01f, 0.08f, 1f),
            1 => new Vector4(0.02f, 0.02f, 0.08f, 1f),
            2 => new Vector4(0.01f, 0.02f, 0.04f, 1f),
            3 => new Vector4(0.0f, 0.0f, 0.03f, 1f),
            4 => new Vector4(0.03f, 0.0f, 0.05f, 1f),
            5 => new Vector4(0.0f, 0.03f, 0.06f, 1f),
            6 => new Vector4(0.01f, 0.01f, 0.06f, 1f),
            7 => new Vector4(0.0f, 0.02f, 0.06f, 1f),
            8 => new Vector4(0.02f, 0.0f, 0.03f, 1f),
            _ => new Vector4(0.02f, 0.01f, 0.07f, 1f),
        };
    }

    private void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float fade, float resolutionScale)
    {
        switch (_effectIndex)
        {
            case 0: DrawMultiWaves(renderer, w, drawH, t, fade, resolutionScale); break;
            case 1: DrawPlasma(renderer, w, drawH, t, fade, resolutionScale * 0.25f); break;
            case 2: DrawCopperBars(renderer, w, drawH, t, fade); break;
            case 3: DrawStarfield(renderer, w, drawH, t, fade); break;
            case 4: DrawBobs(renderer, w, drawH, t, fade); break;
            case 5: DrawSineLandscape(renderer, w, drawH, t, fade, resolutionScale); break;
            case 6: DrawLissajous(renderer, w, drawH, t, fade); break;
            case 7: DrawTunnel(renderer, w, drawH, t, fade, resolutionScale); break;
            case 8: DrawInterference(renderer, w, drawH, t, fade, resolutionScale); break;
            case 9: DrawDotRotator(renderer, w, drawH, t, fade, resolutionScale); break;
        }
    }

    private static void DrawMultiWaves(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var cy = drawH * 0.5f;
        var xStep = Math.Max(6, (int)(6f * resolutionScale));
        (float freq, float amp, float speed, float hue)[] waves =
        [
            (0.012f, 80f, 1.1f, 10f),
            (0.015f, 58f, 0.9f, 65f),
            (0.02f, 42f, 1.4f, 140f),
            (0.026f, 32f, 1.9f, 210f),
            (0.034f, 24f, 2.2f, 285f),
        ];

        foreach (var (freq, amp, speed, hue0) in waves)
        {
            var c = HsvToRgb((hue0 + t * 50f) % 360f, 1f, 1f);
            c.W = alpha * 0.9f;

            for (var x = 0; x < w - xStep; x += xStep)
            {
                var y1 = cy + amp * MathF.Sin(x * freq + t * speed);
                var y2 = cy + amp * MathF.Sin((x + xStep) * freq + t * speed);
                renderer.DrawLine(x, y1, x + xStep, y2, 2f, c);
            }
        }
    }

    private static void DrawPlasma(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var step = Math.Max(14, (int)(14f * resolutionScale));
        for (var y = 0; y < drawH; y += step)
        {
            for (var x = 0; x < w; x += step)
            {
                var v = MathF.Sin(x * 0.03f + t * 1.4f)
                        + MathF.Sin(y * 0.035f + t * 1.2f)
                        + MathF.Sin((x + y) * 0.02f + t);
                var hue = ((v + 3f) / 6f * 360f + t * 60f) % 360f;
                var c = HsvToRgb(hue, 1f, 1f);
                c.W = alpha * 0.88f;
                renderer.FillRect(x, y, step + 1, step + 1, c);
            }
        }
    }

    private static void DrawCopperBars(Dx12Renderer renderer, int w, int drawH, float t, float alpha)
    {
        for (var b = 0; b < 8; b++)
        {
            var phase = t * (1f + b * 0.15f) + b * 0.8f;
            var center = (int)(drawH * 0.5f + drawH * 0.38f * MathF.Sin(phase));
            var barH = 28 + (int)(22 * MathF.Abs(MathF.Sin(t * 0.7f + b)));
            var baseHue = (b * 42f + t * 55f) % 360f;

            for (var row = 0; row < barH; row++)
            {
                var y = center - barH / 2 + row;
                if (y < 0 || y >= drawH)
                {
                    continue;
                }

                var f = row / (float)barH;
                var c = HsvToRgb((baseHue + f * 90f) % 360f, 1f, 0.45f + 0.55f * MathF.Sin(f * MathF.PI));
                c.W = alpha;
                renderer.FillRect(0, y, w, 1, c);
            }
        }
    }

    private void DrawStarfield(Dx12Renderer renderer, int w, int drawH, float t, float alpha)
    {
        var cx = w * 0.5f;
        var cy = drawH * 0.5f;

        foreach (var s in _stars)
        {
            if (s.Z <= 0f)
            {
                continue;
            }

            var sx = (s.X / s.Z) * w * 0.46f + cx + 10f * MathF.Sin(t * 0.9f + s.Y * 4f);
            var sy = (s.Y / s.Z) * drawH * 0.46f + cy;
            if (sx < 0 || sx >= w || sy < 0 || sy >= drawH)
            {
                continue;
            }

            var b = 1f - s.Z;
            var c = HsvToRgb((s.Z * 220f + 170f) % 360f, 0.3f + b * 0.7f, 1f);
            c.W = alpha * b;
            renderer.FillCircle(sx, sy, 1f + b * 3f, c);
        }
    }

    private static void DrawBobs(Dx12Renderer renderer, int w, int drawH, float t, float alpha)
    {
        const int bobCount = 12;
        for (var i = 0; i < bobCount; i++)
        {
            var phase = i * MathF.Tau / bobCount;
            var bx = w * 0.5f + w * 0.4f * MathF.Sin(t * (0.8f + i * 0.08f) + phase);
            var by = drawH * 0.5f + drawH * 0.35f * MathF.Cos(t * (0.7f + i * 0.06f) + phase * 1.3f);

            var c = HsvToRgb((i * 30f + t * 70f) % 360f, 1f, 1f);
            for (var g = 4; g >= 1; g--)
            {
                var glow = c;
                glow.W = alpha * 0.1f * g;
                renderer.FillCircle(bx, by, 12f + g * 5f, glow);
            }

            c.W = alpha;
            renderer.FillCircle(bx, by, 11f, c);
        }
    }

    private static void DrawSineLandscape(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var cols = Math.Max(26, (int)(48f / resolutionScale));
        var rows = Math.Max(18, (int)(30f / resolutionScale));
        var horizon = drawH * 0.24f;
        var amplitude = 72f;

        for (var r = 0; r < rows; r++)
        {
            var depth = r / (float)(rows - 1);
            var yBase = horizon + depth * (drawH - horizon - 20f);
            var thick = 0.9f + depth * 1.7f;
            var xOffset = MathF.Sin(t * 0.6f + depth * 3.2f) * w * 0.03f;

            for (var c = 0; c < cols - 1; c++)
            {
                var x0 = c * (w / (float)(cols - 1));
                var x1 = (c + 1) * (w / (float)(cols - 1));
                x0 += xOffset;
                x1 += xOffset;

                var ridge0 = MathF.Abs(MathF.Sin(c * 0.9f + r * 0.65f + t * 0.85f));
                var ridge1 = MathF.Abs(MathF.Sin((c + 1) * 0.9f + r * 0.65f + t * 0.85f));

                var wave0 = 0.65f * MathF.Sin(c * 0.45f + t * 1.35f + r * 0.2f)
                            + 0.35f * MathF.Sin(c * 0.95f + t * 0.7f - r * 0.33f);
                var wave1 = 0.65f * MathF.Sin((c + 1) * 0.45f + t * 1.35f + r * 0.2f)
                            + 0.35f * MathF.Sin((c + 1) * 0.95f + t * 0.7f - r * 0.33f);

                var h0 = amplitude * (1f - depth) * (wave0 + 0.22f * ridge0);
                var h1 = amplitude * (1f - depth) * (wave1 + 0.22f * ridge1);

                var y0 = yBase + h0;
                var y1 = yBase + h1;

                var col = HsvToRgb((200f + depth * 120f + t * 20f + c * 0.6f) % 360f, 0.9f, 0.45f + (1f - depth) * 0.55f);
                col.W = alpha * (0.45f + (1f - depth) * 0.55f);
                renderer.DrawLine(x0, y0, x1, y1, thick, col);
            }
        }

        var colStep = Math.Max(2, cols / 12);
        for (var c = 0; c < cols; c += colStep)
        {
            var x = c * (w / (float)(cols - 1));
            for (var r = 0; r < rows - 1; r++)
            {
                var depth0 = r / (float)(rows - 1);
                var depth1 = (r + 1) / (float)(rows - 1);
                var yBase0 = horizon + depth0 * (drawH - horizon - 20f);
                var yBase1 = horizon + depth1 * (drawH - horizon - 20f);
                var xOffset0 = MathF.Sin(t * 0.6f + depth0 * 3.2f) * w * 0.03f;
                var xOffset1 = MathF.Sin(t * 0.6f + depth1 * 3.2f) * w * 0.03f;

                var ridge0 = MathF.Abs(MathF.Sin(c * 0.9f + r * 0.65f + t * 0.85f));
                var ridge1 = MathF.Abs(MathF.Sin(c * 0.9f + (r + 1) * 0.65f + t * 0.85f));
                var wave0 = 0.65f * MathF.Sin(c * 0.45f + t * 1.35f + r * 0.2f)
                            + 0.35f * MathF.Sin(c * 0.95f + t * 0.7f - r * 0.33f);
                var wave1 = 0.65f * MathF.Sin(c * 0.45f + t * 1.35f + (r + 1) * 0.2f)
                            + 0.35f * MathF.Sin(c * 0.95f + t * 0.7f - (r + 1) * 0.33f);

                var y0 = yBase0 + amplitude * (1f - depth0) * (wave0 + 0.22f * ridge0);
                var y1 = yBase1 + amplitude * (1f - depth1) * (wave1 + 0.22f * ridge1);

                var col = HsvToRgb((210f + depth0 * 100f + t * 15f) % 360f, 0.7f, 0.35f + (1f - depth0) * 0.45f);
                col.W = alpha * (0.35f + (1f - depth0) * 0.4f);
                renderer.DrawLine(x + xOffset0, y0, x + xOffset1, y1, 0.9f + depth0 * 1.2f, col);
            }
        }
    }

    private void DrawLissajous(Dx12Renderer renderer, int w, int drawH, float t, float alpha)
    {
        var cx = w * 0.5f;
        var cy = drawH * 0.5f;
        var rx = w * 0.42f;
        var ry = drawH * 0.42f;

        var lx = cx + rx * MathF.Sin(3f * t + t * 0.4f);
        var ly = cy + ry * MathF.Sin(2f * t);
        _trail[_trailHead] = new Vector2(lx, ly);
        _trailHead = (_trailHead + 1) % TrailLength;

        for (var i = 0; i < TrailLength - 1; i++)
        {
            var i0 = (_trailHead + i) % TrailLength;
            var i1 = (_trailHead + i + 1) % TrailLength;

            var p0 = _trail[i0];
            var p1 = _trail[i1];
            if (p0 == Vector2.Zero || p1 == Vector2.Zero)
            {
                continue;
            }

            var age = i / (float)TrailLength;
            var c = HsvToRgb((age * 360f + t * 90f) % 360f, 1f, 1f);
            c.W = age * alpha * 0.9f;
            renderer.DrawLine(p0.X, p0.Y, p1.X, p1.Y, 2f, c);
        }

        renderer.FillCircle(lx, ly, 5f, new Vector4(1f, 1f, 1f, alpha));
    }

    private static void DrawTunnel(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var cx = w * 0.5f + MathF.Sin(t * 0.7f) * 80f;
        var cy = drawH * 0.5f + MathF.Cos(t * 0.5f) * 60f;
        var maxR = MathF.Sqrt(w * w + drawH * drawH) * 0.55f;
        var rings = Math.Max(22, (int)(42f / resolutionScale));

        for (var i = 0; i < rings; i++)
        {
            var f = i / (float)rings;
            var r = (f * maxR + (t * 240f)) % maxR;
            var c = HsvToRgb((f * 360f + t * 80f) % 360f, 1f, 1f);
            c.W = alpha * (1f - f) * 0.9f;
            renderer.DrawEllipse(cx - r, cy - r, r * 2, r * 2, 2f, c);
        }
    }

    private static void DrawInterference(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var cx1 = w * (0.35f + 0.12f * MathF.Sin(t * 0.6f));
        var cy1 = drawH * (0.5f + 0.15f * MathF.Cos(t * 0.7f));
        var cx2 = w * (0.65f + 0.12f * MathF.Cos(t * 0.8f));
        var cy2 = drawH * (0.5f - 0.15f * MathF.Sin(t * 0.5f));

        var maxR = (int)(MathF.Sqrt(w * w + drawH * drawH) * 0.45f);
        var ringStep = Math.Max(14, (int)(14f * resolutionScale));
        for (var r = 8; r < maxR; r += ringStep)
        {
            var wave = 0.5f + 0.5f * MathF.Sin(r * 0.08f - t * 2.4f);
            if (wave < 0.4f)
            {
                continue;
            }

            var c1 = HsvToRgb((r * 1.2f + t * 55f) % 360f, 1f, 1f);
            c1.W = alpha * wave * 0.8f;
            renderer.DrawEllipse(cx1 - r, cy1 - r, r * 2, r * 2, 2f, c1);

            var c2 = HsvToRgb((r * 1.2f + t * 55f + 150f) % 360f, 1f, 1f);
            c2.W = alpha * wave * 0.8f;
            renderer.DrawEllipse(cx2 - r, cy2 - r, r * 2, r * 2, 2f, c2);
        }
    }

    private static void DrawDotRotator(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var cx = w * 0.5f;
        var cy = drawH * 0.5f;

        var uCount = Math.Max(14, (int)(28f / resolutionScale));
        var vCount = Math.Max(8, (int)(14f / resolutionScale));
        var R = 200f;
        var r = 80f;
        var rotX = t * 0.5f;
        var rotY = t * 0.75f;

        for (var u = 0; u < uCount; u++)
        {
            for (var v = 0; v < vCount; v++)
            {
                var au = u * MathF.Tau / uCount;
                var av = v * MathF.Tau / vCount;

                var disp = 12f * MathF.Sin(au * 3f + t * 2.2f) * MathF.Cos(av * 2f + t * 1.6f);
                var x = (R + r * MathF.Cos(av) + disp) * MathF.Cos(au);
                var y = (R + r * MathF.Cos(av) + disp) * MathF.Sin(au);
                var z = r * MathF.Sin(av);

                var y2 = y * MathF.Cos(rotX) - z * MathF.Sin(rotX);
                var z2 = y * MathF.Sin(rotX) + z * MathF.Cos(rotX);
                var x3 = x * MathF.Cos(rotY) + z2 * MathF.Sin(rotY);
                var z3 = -x * MathF.Sin(rotY) + z2 * MathF.Cos(rotY);

                var depth = 620f;
                var pz = z3 + depth;
                if (pz <= 0.01f)
                {
                    continue;
                }

                var px = cx + x3 * depth / pz;
                var py = cy + y2 * depth / pz;

                var c = HsvToRgb((u * 13f + v * 21f + t * 60f) % 360f, 1f, 1f);
                c.W = alpha * 0.9f;
                var dotR = Math.Max(1.4f, (depth / pz) * 2.8f);
                renderer.FillCircle(px, py, dotR, c);
            }
        }
    }

    private static void DrawScroller(Dx12Renderer renderer, int w, int h, float t)
    {
        const float barH = 78f;
        var barY = h - barH;

        for (var row = 0; row < barH; row++)
        {
            var f = row / barH;
            renderer.FillRect(0, barY + row, w, 1,
                new Vector4(0.02f + f * 0.12f, 0.06f + f * 0.08f, 0.12f + f * 0.16f, 1f));
        }

        renderer.FillRect(0, barY, w, 2, new Vector4(0.8f, 1f, 0.8f, 0.8f));

        const string text = "*** DIRECTX12 SINUS FX *** MULTI-WAVES * PLASMA * COPPER * STARFIELD * BOBS * LANDSCAPE * LISSAJOUS * TUNNEL * INTERFERENCE * DOT ROTATOR ***";
        const float speed = 210f;
        var textY = barY + 22f + 5f * MathF.Sin(t * 3f);

        var textWidth = renderer.MeasureText(text, 28f).Width;
        const float gap = 140f;
        var loop = w + textWidth + gap;
        var x = w - (t * speed % loop);
        renderer.DrawText(text, x, textY, 28f, new Vector4(1f, 0.9f, 0.35f, 0.95f));
        renderer.DrawText(text, x + textWidth + gap, textY, 28f, new Vector4(0.45f, 1f, 1f, 0.95f));
    }

    private void DrawLabel(Dx12Renderer renderer, int w)
    {
        string[] labels =
        [
            "1. MULTI-LAYER SINE WAVES",
            "2. CLASSIC PLASMA",
            "3. COPPER BARS",
            "4. STARFIELD",
            "5. BOUNCING BOBS",
            "6. SINE LANDSCAPE",
            "7. LISSAJOUS CURVES",
            "8. TUNNEL EFFECT",
            "9. INTERFERENCE RINGS",
            "10. DOT ROTATOR",
        ];

        var idx = Math.Clamp(_effectIndex, 0, labels.Length - 1);
        if (_labelWidths[idx] <= 0f)
        {
            _labelWidths[idx] = renderer.MeasureText(labels[idx], 20f).Width;
        }

        renderer.DrawText(labels[idx], (w - _labelWidths[idx]) * 0.5f, 12f, 20f, new Vector4(1f, 1f, 0.8f, 0.85f));
    }

    private static void DrawScanlines(Dx12Renderer renderer, int w, int h, float resolutionScale)
    {
        Vector4 sc = new(0f, 0f, 0f, 0.18f);
        var step = Math.Max(3, (int)(3f * resolutionScale));
        for (var y = 0; y < h; y += step)
        {
            renderer.FillRect(0, y, w, 1, sc);
        }
    }

    private static Vector4 HsvToRgb(float h, float s, float v)
    {
        var c = v * s;
        var x = c * (1 - MathF.Abs((h / 60f % 2) - 1));
        var m = v - c;

        var r = 0f;
        var g = 0f;
        var b = 0f;
        if (h < 60f) { r = c; g = x; }
        else if (h < 120f) { r = x; g = c; }
        else if (h < 180f) { g = c; b = x; }
        else if (h < 240f) { g = x; b = c; }
        else if (h < 300f) { r = x; b = c; }
        else { r = c; b = x; }

        return new Vector4(r + m, g + m, b + m, 1f);
    }

    public void Dispose()
    {
    }
}