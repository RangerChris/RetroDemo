using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusTunnelScene : SinusSceneBase
{
    public SinusTunnelScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.0f, 0.02f, 0.06f, 1f);

    protected override string Label => "8. TUNNEL EFFECT";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var centerX = w * 0.5f + MathF.Sin(t * 0.7f) * 80f;
        var centerY = drawH * 0.5f + MathF.Cos(t * 0.5f) * 60f;
        var maxRadius = MathF.Sqrt(w * w + drawH * drawH) * 0.55f;
        var ringCount = Math.Max(22, (int)(42f / resolutionScale));

        for (var i = 0; i < ringCount; i++)
        {
            var ringT = i / (float)ringCount;
            var radius = (ringT * maxRadius + (t * 240f)) % maxRadius;
            var color = HsvToRgb((ringT * 360f + t * 80f) % 360f, 1f, 1f);
            color.W = alpha * (1f - ringT) * 0.9f;
            renderer.DrawEllipse(centerX - radius, centerY - radius, radius * 2, radius * 2, 2f, color);
        }
    }
}
