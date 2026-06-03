using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusBobsScene : SinusSceneBase
{
    public SinusBobsScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.03f, 0.0f, 0.05f, 1f);

    protected override string Label => "5. BOUNCING BOBS";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        const int bobCount = 12;
        for (var i = 0; i < bobCount; i++)
        {
            var phaseOffset = i * MathF.Tau / bobCount;
            var bobX = w * 0.5f + w * 0.4f * MathF.Sin(t * (0.8f + i * 0.08f) + phaseOffset);
            var bobY = drawH * 0.5f + drawH * 0.35f * MathF.Cos(t * (0.7f + i * 0.06f) + phaseOffset * 1.3f);

            var color = HsvToRgb((i * 30f + t * 70f) % 360f, 1f, 1f);
            for (var glowLayer = 4; glowLayer >= 1; glowLayer--)
            {
                var glowColor = color;
                glowColor.W = alpha * 0.1f * glowLayer;
                renderer.FillCircle(bobX, bobY, 12f + glowLayer * 5f, glowColor);
            }

            color.W = alpha;
            renderer.FillCircle(bobX, bobY, 11f, color);
        }
    }
}
