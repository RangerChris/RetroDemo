using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusLandscapeScene : SinusSceneBase
{
    public SinusLandscapeScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.0f, 0.03f, 0.06f, 1f);

    protected override string Label => "6. SINE LANDSCAPE";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var columnCount = Math.Max(26, (int)(48f / resolutionScale));
        var rowCount = Math.Max(18, (int)(30f / resolutionScale));
        var horizon = drawH * 0.24f;
        var amplitude = 72f;

        for (var row = 0; row < rowCount; row++)
        {
            var depth = row / (float)(rowCount - 1);
            var rowBaseY = horizon + depth * (drawH - horizon - 20f);
            var lineThickness = 0.9f + depth * 1.7f;
            var rowOffset = MathF.Sin(t * 0.6f + depth * 3.2f) * w * 0.03f;

            for (var col = 0; col < columnCount - 1; col++)
            {
                var xLeft = col * (w / (float)(columnCount - 1));
                var xRight = (col + 1) * (w / (float)(columnCount - 1));
                xLeft += rowOffset;
                xRight += rowOffset;

                var ridge0 = MathF.Abs(MathF.Sin(col * 0.9f + row * 0.65f + t * 0.85f));
                var ridge1 = MathF.Abs(MathF.Sin((col + 1) * 0.9f + row * 0.65f + t * 0.85f));

                var wave0 = 0.65f * MathF.Sin(col * 0.45f + t * 1.35f + row * 0.2f)
                            + 0.35f * MathF.Sin(col * 0.95f + t * 0.7f - row * 0.33f);
                var wave1 = 0.65f * MathF.Sin((col + 1) * 0.45f + t * 1.35f + row * 0.2f)
                            + 0.35f * MathF.Sin((col + 1) * 0.95f + t * 0.7f - row * 0.33f);

                var height0 = amplitude * (1f - depth) * (wave0 + 0.22f * ridge0);
                var height1 = amplitude * (1f - depth) * (wave1 + 0.22f * ridge1);

                var yLeft = rowBaseY + height0;
                var yRight = rowBaseY + height1;

                var color = HsvToRgb((200f + depth * 120f + t * 20f + col * 0.6f) % 360f, 0.9f, 0.45f + (1f - depth) * 0.55f);
                color.W = alpha * (0.45f + (1f - depth) * 0.55f);
                renderer.DrawLine(xLeft, yLeft, xRight, yRight, lineThickness, color);
            }
        }

        var columnStep = Math.Max(2, columnCount / 12);
        for (var col = 0; col < columnCount; col += columnStep)
        {
            var columnX = col * (w / (float)(columnCount - 1));
            for (var row = 0; row < rowCount - 1; row++)
            {
                var depth0 = row / (float)(rowCount - 1);
                var depth1 = (row + 1) / (float)(rowCount - 1);
                var rowBaseY0 = horizon + depth0 * (drawH - horizon - 20f);
                var rowBaseY1 = horizon + depth1 * (drawH - horizon - 20f);
                var rowOffset0 = MathF.Sin(t * 0.6f + depth0 * 3.2f) * w * 0.03f;
                var rowOffset1 = MathF.Sin(t * 0.6f + depth1 * 3.2f) * w * 0.03f;

                var ridge0 = MathF.Abs(MathF.Sin(col * 0.9f + row * 0.65f + t * 0.85f));
                var ridge1 = MathF.Abs(MathF.Sin(col * 0.9f + (row + 1) * 0.65f + t * 0.85f));
                var wave0 = 0.65f * MathF.Sin(col * 0.45f + t * 1.35f + row * 0.2f)
                            + 0.35f * MathF.Sin(col * 0.95f + t * 0.7f - row * 0.33f);
                var wave1 = 0.65f * MathF.Sin(col * 0.45f + t * 1.35f + (row + 1) * 0.2f)
                            + 0.35f * MathF.Sin(col * 0.95f + t * 0.7f - (row + 1) * 0.33f);

                var y0 = rowBaseY0 + amplitude * (1f - depth0) * (wave0 + 0.22f * ridge0);
                var y1 = rowBaseY1 + amplitude * (1f - depth1) * (wave1 + 0.22f * ridge1);

                var color = HsvToRgb((210f + depth0 * 100f + t * 15f) % 360f, 0.7f, 0.35f + (1f - depth0) * 0.45f);
                color.W = alpha * (0.35f + (1f - depth0) * 0.4f);
                renderer.DrawLine(columnX + rowOffset0, y0, columnX + rowOffset1, y1, 0.9f + depth0 * 1.2f, color);
            }
        }
    }
}
