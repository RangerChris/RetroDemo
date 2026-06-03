using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusLissajousScene : SinusSceneBase
{
    private const int TrailLength = 220;
    private readonly Vector2[] _trail = new Vector2[TrailLength];
    private int _trailHead;

    public SinusLissajousScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.01f, 0.01f, 0.06f, 1f);

    protected override string Label => "7. LISSAJOUS CURVES";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var centerX = w * 0.5f;
        var centerY = drawH * 0.5f;
        var radiusX = w * 0.42f;
        var radiusY = drawH * 0.42f;

        var pointX = centerX + radiusX * MathF.Sin(3f * t + t * 0.4f);
        var pointY = centerY + radiusY * MathF.Sin(2f * t);
        _trail[_trailHead] = new Vector2(pointX, pointY);
        _trailHead = (_trailHead + 1) % TrailLength;

        for (var i = 0; i < TrailLength - 1; i++)
        {
            var trailIndex0 = (_trailHead + i) % TrailLength;
            var trailIndex1 = (_trailHead + i + 1) % TrailLength;

            var point0 = _trail[trailIndex0];
            var point1 = _trail[trailIndex1];
            if (point0 == Vector2.Zero || point1 == Vector2.Zero)
            {
                continue;
            }

            var age = i / (float)TrailLength;
            var color = HsvToRgb((age * 360f + t * 90f) % 360f, 1f, 1f);
            color.W = age * alpha * 0.9f;
            renderer.DrawLine(point0.X, point0.Y, point1.X, point1.Y, 2f, color);
        }

        renderer.FillCircle(pointX, pointY, 5f, new Vector4(1f, 1f, 1f, alpha));
    }
}
