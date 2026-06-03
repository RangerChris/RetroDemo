using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusInterferenceScene : SinusSceneBase
{
    public SinusInterferenceScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.02f, 0.0f, 0.03f, 1f);

    protected override string Label => "9. INTERFERENCE RINGS";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var centerX1 = w * (0.35f + 0.12f * MathF.Sin(t * 0.6f));
        var centerY1 = drawH * (0.5f + 0.15f * MathF.Cos(t * 0.7f));
        var centerX2 = w * (0.65f + 0.12f * MathF.Cos(t * 0.8f));
        var centerY2 = drawH * (0.5f - 0.15f * MathF.Sin(t * 0.5f));

        var maxRadius = (int)(MathF.Sqrt(w * w + drawH * drawH) * 0.45f);
        var ringStepSize = Math.Max(14, (int)(14f * resolutionScale));
        for (var radius = 8; radius < maxRadius; radius += ringStepSize)
        {
            var waveStrength = 0.5f + 0.5f * MathF.Sin(radius * 0.08f - t * 2.4f);
            if (waveStrength < 0.4f)
            {
                continue;
            }

            var color1 = HsvToRgb((radius * 1.2f + t * 55f) % 360f, 1f, 1f);
            color1.W = alpha * waveStrength * 0.8f;
            renderer.DrawEllipse(centerX1 - radius, centerY1 - radius, radius * 2, radius * 2, 2f, color1);

            var color2 = HsvToRgb((radius * 1.2f + t * 55f + 150f) % 360f, 1f, 1f);
            color2.W = alpha * waveStrength * 0.8f;
            renderer.DrawEllipse(centerX2 - radius, centerY2 - radius, radius * 2, radius * 2, 2f, color2);
        }
    }
}
