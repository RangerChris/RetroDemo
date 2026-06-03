using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusPlasmaScene : SinusSceneBase
{
    public SinusPlasmaScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.02f, 0.02f, 0.08f, 1f);

    protected override string Label => "2. CLASSIC PLASMA";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var cellSize = Math.Max(14, (int)(14f * resolutionScale * 0.25f));
        for (var y = 0; y < drawH; y += cellSize)
        {
            for (var x = 0; x < w; x += cellSize)
            {
                var value = MathF.Sin(x * 0.03f + t * 1.4f)
                        + MathF.Sin(y * 0.035f + t * 1.2f)
                        + MathF.Sin((x + y) * 0.02f + t);
                var hue = ((value + 3f) / 6f * 360f + t * 60f) % 360f;
                var color = HsvToRgb(hue, 1f, 1f);
                color.W = alpha * 0.88f;
                renderer.FillRect(x, y, cellSize + 1, cellSize + 1, color);
            }
        }
    }
}
