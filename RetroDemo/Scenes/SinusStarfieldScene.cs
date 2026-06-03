using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusStarfieldScene : SinusSceneBase
{
    private const int StarCount = 180;
    private readonly Star[] _stars = new Star[StarCount];
    private readonly Random _rng = new(77);

    private struct Star
    {
        public float X;
        public float Y;
        public float Z;
    }

    public SinusStarfieldScene(int w, int h)
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

    protected override Vector4 BackgroundColor => new(0.0f, 0.0f, 0.03f, 1f);

    protected override string Label => "4. STARFIELD";

    protected override void UpdateEffect(float deltaTime)
    {
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
    }

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var centerX = w * 0.5f;
        var centerY = drawH * 0.5f;

        foreach (var star in _stars)
        {
            if (star.Z <= 0f)
            {
                continue;
            }

            var screenX = (star.X / star.Z) * w * 0.46f + centerX + 10f * MathF.Sin(t * 0.9f + star.Y * 4f);
            var screenY = (star.Y / star.Z) * drawH * 0.46f + centerY;
            if (screenX < 0 || screenX >= w || screenY < 0 || screenY >= drawH)
            {
                continue;
            }

            var brightness = 1f - star.Z;
            var color = HsvToRgb((star.Z * 220f + 170f) % 360f, 0.3f + brightness * 0.7f, 1f);
            color.W = alpha * brightness;
            renderer.FillCircle(screenX, screenY, 1f + brightness * 3f, color);
        }
    }
}
