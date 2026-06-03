using System.Numerics;

namespace RetroDemo.Scenes;

public abstract class SinusSceneBase : IScene
{
    private const float EffectDuration = 5.0f;
    private static float _scrollerTime;

    private float _time;
    private float _effectTime;
    private float _labelWidth;

    protected float Time => _time;

    public bool Update(float deltaTime)
    {
        _time += deltaTime;
        _effectTime += deltaTime;
        _scrollerTime += deltaTime;
        UpdateEffect(deltaTime);
        return _effectTime >= EffectDuration;
    }

    public void Draw(Dx12Renderer renderer)
    {
        var width = renderer.Width;
        var height = renderer.Height;
        var drawHeight = height - 86;
        var resolutionScale = MathF.Max(1f, MathF.Max(width / 1920f, drawHeight / 1080f));

        var fade = GetEffectFade();

        renderer.Clear(BackgroundColor);
        renderer.BeginOverlay();

        DrawEffect(renderer, width, drawHeight, _time, fade, resolutionScale);
        DrawScroller(renderer, width, height, _scrollerTime);
        DrawLabel(renderer, width);
        DrawScanlines(renderer, width, height, resolutionScale);

        renderer.EndOverlay();
    }

    protected virtual void UpdateEffect(float deltaTime)
    {
    }

    protected abstract Vector4 BackgroundColor { get; }

    protected abstract string Label { get; }

    protected abstract void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale);

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

    private void DrawLabel(Dx12Renderer renderer, int w)
    {
        if (_labelWidth <= 0f)
        {
            _labelWidth = renderer.MeasureText(Label, 20f).Width;
        }

        renderer.DrawText(Label, (w - _labelWidth) * 0.5f, 12f, 20f, new Vector4(1f, 1f, 0.8f, 0.85f));
    }

    private static void DrawScroller(Dx12Renderer renderer, int w, int h, float t)
    {
        const float barHeight = 78f;
        var barTop = h - barHeight;

        for (var row = 0; row < barHeight; row++)
        {
            var rowT = row / barHeight;
            renderer.FillRect(0, barTop + row, w, 1,
                new Vector4(0.02f + rowT * 0.12f, 0.06f + rowT * 0.08f, 0.12f + rowT * 0.16f, 1f));
        }

        renderer.FillRect(0, barTop, w, 2, new Vector4(0.8f, 1f, 0.8f, 0.8f));

        const string text = "*** DIRECTX12 SINUS FX *** MULTI-WAVES * PLASMA * COPPER * STARFIELD * BOBS * LANDSCAPE * LISSAJOUS * TUNNEL * INTERFERENCE * DOT ROTATOR ***";
        const float speed = 210f;
        var textBaseline = barTop + 22f + 5f * MathF.Sin(t * 3f);

        var textPixelWidth = renderer.MeasureText(text, 28f).Width;
        const float gap = 140f;
        var loopWidth = w + textPixelWidth + gap;
        var textX = w - (t * speed % loopWidth);
        renderer.DrawText(text, textX, textBaseline, 28f, new Vector4(1f, 0.9f, 0.35f, 0.95f));
        renderer.DrawText(text, textX + textPixelWidth + gap, textBaseline, 28f, new Vector4(0.45f, 1f, 1f, 0.95f));
    }

    private static void DrawScanlines(Dx12Renderer renderer, int w, int h, float resolutionScale)
    {
        Vector4 scanlineColor = new(0f, 0f, 0f, 0.18f);
        var lineStep = Math.Max(3, (int)(3f * resolutionScale));
        for (var y = 0; y < h; y += lineStep)
        {
            renderer.FillRect(0, y, w, 1, scanlineColor);
        }
    }

    protected static Vector4 HsvToRgb(float h, float s, float v)
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

    public virtual void Dispose()
    {
    }
}
