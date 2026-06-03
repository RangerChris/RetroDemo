using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusCopperBarsScene : SinusSceneBase
{
    public SinusCopperBarsScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.01f, 0.02f, 0.04f, 1f);

    protected override string Label => "3. COPPER BARS";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        for (var barIndex = 0; barIndex < 8; barIndex++)
        {
            var phase = t * (1f + barIndex * 0.15f) + barIndex * 0.8f;
            var centerY = (int)(drawH * 0.5f + drawH * 0.38f * MathF.Sin(phase));
            var barHeight = 28 + (int)(22 * MathF.Abs(MathF.Sin(t * 0.7f + barIndex)));
            var baseHue = (barIndex * 42f + t * 55f) % 360f;

            for (var row = 0; row < barHeight; row++)
            {
                var y = centerY - barHeight / 2 + row;
                if (y < 0 || y >= drawH)
                {
                    continue;
                }

                var rowT = row / (float)barHeight;
                var color = HsvToRgb((baseHue + rowT * 90f) % 360f, 1f, 0.45f + 0.55f * MathF.Sin(rowT * MathF.PI));
                color.W = alpha;
                renderer.FillRect(0, y, w, 1, color);
            }
        }
    }
}
