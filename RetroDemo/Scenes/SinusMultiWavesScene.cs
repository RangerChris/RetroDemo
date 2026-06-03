using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusMultiWavesScene : SinusSceneBase
{
    public SinusMultiWavesScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.03f, 0.01f, 0.08f, 1f);

    protected override string Label => "1. MULTI-LAYER SINE WAVES";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var centerY = drawH * 0.5f;
        var xStep = Math.Max(6, (int)(6f * resolutionScale));
        (float freq, float amp, float speed, float hue)[] waves =
        [
            (0.012f, 80f, 0.6f, 10f),
            (0.015f, 58f, -1.1f, 65f),
            (0.02f, 42f, 1.8f, 140f),
            (0.026f, 32f, -2.6f, 210f),
            (0.034f, 24f, 3.4f, 285f),
        ];

        foreach (var (freq, amp, speed, hue0) in waves)
        {
            var color = HsvToRgb((hue0 + t * 50f) % 360f, 1f, 1f);
            color.W = alpha * 0.9f;

            for (var x = 0; x < w - xStep; x += xStep)
            {
                var yStart = centerY + amp * MathF.Sin(x * freq + t * speed);
                var yEnd = centerY + amp * MathF.Sin((x + xStep) * freq + t * speed);
                renderer.DrawLine(x, yStart, x + xStep, yEnd, 2f, color);
            }
        }
    }
}
