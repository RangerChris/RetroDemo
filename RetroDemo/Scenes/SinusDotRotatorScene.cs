using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class SinusDotRotatorScene : SinusSceneBase
{
    public SinusDotRotatorScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    protected override Vector4 BackgroundColor => new(0.02f, 0.01f, 0.07f, 1f);

    protected override string Label => "10. DOT ROTATOR";

    protected override void DrawEffect(Dx12Renderer renderer, int w, int drawH, float t, float alpha, float resolutionScale)
    {
        var centerX = w * 0.5f;
        var centerY = drawH * 0.5f;

        var majorSegments = Math.Max(14, (int)(28f / resolutionScale));
        var minorSegments = Math.Max(8, (int)(14f / resolutionScale));
        var majorRadius = 200f;
        var minorRadius = 80f;
        var rotationX = t * 0.5f;
        var rotationY = t * 0.75f;

        for (var uIndex = 0; uIndex < majorSegments; uIndex++)
        {
            for (var vIndex = 0; vIndex < minorSegments; vIndex++)
            {
                var angleU = uIndex * MathF.Tau / majorSegments;
                var angleV = vIndex * MathF.Tau / minorSegments;

                var displacement = 12f * MathF.Sin(angleU * 3f + t * 2.2f) * MathF.Cos(angleV * 2f + t * 1.6f);
                var localX = (majorRadius + minorRadius * MathF.Cos(angleV) + displacement) * MathF.Cos(angleU);
                var localY = (majorRadius + minorRadius * MathF.Cos(angleV) + displacement) * MathF.Sin(angleU);
                var localZ = minorRadius * MathF.Sin(angleV);

                var rotatedY = localY * MathF.Cos(rotationX) - localZ * MathF.Sin(rotationX);
                var rotatedZ = localY * MathF.Sin(rotationX) + localZ * MathF.Cos(rotationX);
                var rotatedX = localX * MathF.Cos(rotationY) + rotatedZ * MathF.Sin(rotationY);
                var rotatedDepth = -localX * MathF.Sin(rotationY) + rotatedZ * MathF.Cos(rotationY);

                var projectionDepth = 620f;
                var projectedZ = rotatedDepth + projectionDepth;
                if (projectedZ <= 0.01f)
                {
                    continue;
                }

                var screenX = centerX + rotatedX * projectionDepth / projectedZ;
                var screenY = centerY + rotatedY * projectionDepth / projectedZ;

                var color = HsvToRgb((uIndex * 13f + vIndex * 21f + t * 60f) % 360f, 1f, 1f);
                color.W = alpha * 0.9f;
                var dotRadius = Math.Max(1.4f, (projectionDepth / projectedZ) * 2.8f);
                renderer.FillCircle(screenX, screenY, dotRadius, color);
            }
        }
    }
}
