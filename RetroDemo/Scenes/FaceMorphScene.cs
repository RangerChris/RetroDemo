using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class FaceMorphScene : IScene
{
    private const float FemaleShowTime = 4.0f;
    private const float MorphDuration = 3.0f;
    private const float RobotShowTime = 3.5f;
    private const float OutroDuration = 0.8f;

    private float _time;
    private float _morphT;
    private float _smileT;
    private float _outroT;

    public FaceMorphScene(int w, int h)
    {
        _ = w;
        _ = h;
    }

    public bool Update(float deltaTime)
    {
        _time += deltaTime;

        float smileStart = FemaleShowTime - 1.5f;
        if (_time >= smileStart)
            _smileT = Math.Clamp((_time - smileStart) / 1.2f, 0f, 1f);

        float morphStart = FemaleShowTime;
        if (_time >= morphStart)
            _morphT = Math.Clamp((_time - morphStart) / MorphDuration, 0f, 1f);

        float outroStart = FemaleShowTime + MorphDuration + RobotShowTime;
        if (_time >= outroStart)
            _outroT = Math.Clamp((_time - outroStart) / OutroDuration, 0f, 1f);

        return _time >= FemaleShowTime + MorphDuration + RobotShowTime + OutroDuration;
    }

    public void Draw(Dx12Renderer renderer)
    {
        int w = renderer.Width;
        int h = renderer.Height;
        float t = _time;

        Vector4 bgA = new(0.03f, 0.02f, 0.08f, 1f);
        Vector4 bgB = new(0.02f, 0.12f, 0.18f, 1f);
        Vector4 bg = Vector4.Lerp(bgA, bgB, _morphT);
        bg.W = 1f;
        renderer.Clear(bg);

        renderer.BeginOverlay();
        DrawBackgroundStars(renderer, w, h, t);

        float centerX = w * 0.5f;
        float centerY = h * 0.5f;
        float faceScale = MathF.Min(w / 1920f, h / 1080f);
        faceScale = Math.Clamp(faceScale, 0.75f, 1.35f);

        float globalAlpha = 1f - _outroT;
        float femaleAlpha = (1f - _morphT) * globalAlpha;
        float robotAlpha = _morphT * globalAlpha;

        float femaleTurnT = Math.Clamp(_time / FemaleShowTime, 0f, 1f);
        float femaleYaw = -22f * femaleTurnT;

        float robotTurnT = Math.Clamp((_time - FemaleShowTime) / MorphDuration, 0f, 1f);
        float robotYaw = 22f * robotTurnT;

        DrawFemaleFace(renderer, centerX, centerY, faceScale, femaleYaw, _smileT, femaleAlpha);
        DrawRobotFace(renderer, centerX, centerY, faceScale, robotYaw, t, robotAlpha);

        DrawGlitchStrips(renderer, w, h, _morphT);
        DrawLabel(renderer, w, h, globalAlpha);
        DrawScanlines(renderer, w, h);
        renderer.EndOverlay();
    }

    private static void DrawBackgroundStars(Dx12Renderer renderer, int w, int h, float t)
    {
        int steps = 80;
        for (int i = 0; i < steps; i++)
        {
            float f = i / (float)steps;
            Vector4 c = new(0.05f + f * 0.03f, 0.05f + f * 0.05f, 0.12f + f * 0.08f, 0.85f);
            renderer.FillRect(0, i * h / steps, w, h / steps + 1, c);
        }

        for (int i = 0; i < 120; i++)
        {
            float sx = i * 113 % w;
            float sy = i * 73 % h;
            float twinkle = 0.35f + 0.65f * (0.5f + 0.5f * MathF.Sin(t * 2.3f + i));
            renderer.FillRect(sx, sy, 2, 2, new Vector4(0.8f, 0.9f, 1f, twinkle * 0.7f));
        }
    }

    private static void DrawFemaleFace(Dx12Renderer renderer, float cx, float cy, float scale, float yawDeg, float smileT, float alpha)
    {
        if (alpha <= 0.001f)
            return;

        float yaw = yawDeg / 30f;
        float xOffset = yaw * 70f * scale;
        float widthScale = 1f - MathF.Abs(yaw) * 0.18f;

        float fw = 340f * scale * widthScale;
        float fh = 430f * scale;
        float fx = cx - fw * 0.5f + xOffset;
        float fy = cy - fh * 0.58f;

        Vector4 hair = new(0.22f, 0.11f, 0.04f, alpha);
        Vector4 skin = new(1.0f, 0.80f, 0.66f, alpha);
        Vector4 blush = new(1f, 0.52f, 0.55f, alpha * (0.15f + smileT * 0.4f));
        Vector4 lip = new(0.86f, 0.39f, 0.35f, alpha);

        renderer.FillEllipse(fx - 20f * scale, fy - 35f * scale, fw + 40f * scale, fh + 70f * scale, hair);
        renderer.FillEllipse(fx, fy, fw, fh, skin);

        float eyeY = fy + fh * 0.38f;
        float eyeDX = fw * 0.22f;
        float pupilShift = yaw * 12f * scale;

        renderer.FillEllipse(cx - eyeDX + xOffset - 23f * scale, eyeY - 16f * scale, 46f * scale, 32f * scale, new Vector4(1f, 1f, 1f, alpha));
        renderer.FillEllipse(cx + eyeDX + xOffset - 23f * scale, eyeY - 16f * scale, 46f * scale, 32f * scale, new Vector4(1f, 1f, 1f, alpha));

        renderer.FillCircle(cx - eyeDX + xOffset + pupilShift, eyeY, 9f * scale, new Vector4(0.31f, 0.51f, 0.9f, alpha));
        renderer.FillCircle(cx + eyeDX + xOffset + pupilShift, eyeY, 9f * scale, new Vector4(0.31f, 0.51f, 0.9f, alpha));
        renderer.FillCircle(cx - eyeDX + xOffset + pupilShift, eyeY, 4f * scale, new Vector4(0f, 0f, 0f, alpha));
        renderer.FillCircle(cx + eyeDX + xOffset + pupilShift, eyeY, 4f * scale, new Vector4(0f, 0f, 0f, alpha));

        renderer.FillEllipse(cx + xOffset - 12f * scale, fy + fh * 0.5f, 24f * scale, 44f * scale, new Vector4(0.87f, 0.67f, 0.55f, alpha));

        float mouthY = fy + fh * 0.75f;
        float smileUp = smileT * 12f * scale;
        renderer.DrawLine(cx - 45f * scale + xOffset, mouthY + smileUp, cx + 45f * scale + xOffset, mouthY + smileUp, 4f * scale, lip);
        renderer.FillEllipse(cx - 56f * scale + xOffset, mouthY + smileUp - 6f * scale, 16f * scale, 16f * scale, lip);
        renderer.FillEllipse(cx + 40f * scale + xOffset, mouthY + smileUp - 6f * scale, 16f * scale, 16f * scale, lip);

        renderer.FillEllipse(cx - 95f * scale + xOffset, fy + fh * 0.58f, 46f * scale, 36f * scale, blush);
        renderer.FillEllipse(cx + 49f * scale + xOffset, fy + fh * 0.58f, 46f * scale, 36f * scale, blush);
    }

    private static void DrawRobotFace(Dx12Renderer renderer, float cx, float cy, float scale, float yawDeg, float t, float alpha)
    {
        if (alpha <= 0.001f)
            return;

        float yaw = yawDeg / 28f;
        float xOffset = yaw * 85f * scale;
        float widthScale = 1f - MathF.Abs(yaw) * 0.22f;

        float hw = 390f * scale * widthScale;
        float hh = 430f * scale;
        float hx = cx - hw * 0.5f + xOffset;
        float hy = cy - hh * 0.58f;

        Vector4 metal = new(0.30f, 0.35f, 0.40f, alpha);
        Vector4 metalDark = new(0.20f, 0.23f, 0.27f, alpha);

        renderer.FillRect(hx, hy, hw, hh, metalDark);
        renderer.DrawEllipse(hx - 6f * scale, hy - 12f * scale, hw + 12f * scale, hh + 24f * scale, 4f * scale, new Vector4(0.45f, 0.55f, 0.62f, alpha));

        renderer.FillRect(hx + hw * 0.1f, hy + hh * 0.18f, hw * 0.8f, hh * 0.15f, new Vector4(0.05f, 0.10f, 0.14f, alpha));
        float eyePulse = 0.4f + 0.6f * (0.5f + 0.5f * MathF.Sin(t * 5f));
        renderer.FillRect(hx + hw * 0.16f, hy + hh * 0.23f, hw * 0.68f, hh * 0.05f, new Vector4(0.35f, 0.95f, 1f, alpha * eyePulse));

        renderer.FillRect(hx + hw * 0.46f, hy + hh * 0.34f, hw * 0.08f, hh * 0.16f, metal);

        renderer.FillRect(hx + hw * 0.15f, hy + hh * 0.62f, hw * 0.70f, hh * 0.23f, metal);
        for (int i = 0; i < 7; i++)
        {
            float barH = (0.05f + 0.04f * (0.5f + 0.5f * MathF.Sin(t * 4f + i))) * hh;
            float bx = hx + hw * 0.24f + i * hw * 0.075f;
            float by = hy + hh * 0.77f - barH;
            renderer.FillRect(bx, by, hw * 0.04f, barH, new Vector4(0.1f, 0.9f, 1f, alpha * 0.9f));
        }

        float sidePulse = 0.5f + 0.5f * MathF.Sin(t * 3.1f + 0.6f);
        renderer.FillEllipse(hx - 38f * scale, hy + hh * 0.35f, 50f * scale, 100f * scale, metal);
        renderer.FillEllipse(hx + hw - 12f * scale, hy + hh * 0.35f, 50f * scale, 100f * scale, metal);
        renderer.FillCircle(hx - 14f * scale, hy + hh * 0.47f, 7f * scale, new Vector4(1f, 0.45f, 0.1f, alpha * sidePulse));
        renderer.FillCircle(hx + hw + 14f * scale, hy + hh * 0.47f, 7f * scale, new Vector4(1f, 0.45f, 0.1f, alpha * sidePulse));

        renderer.FillRect(cx - 70f * scale + xOffset, hy + hh + 8f * scale, 140f * scale, 24f * scale, metalDark);
    }

    private static void DrawGlitchStrips(Dx12Renderer renderer, int w, int h, float morphT)
    {
        if (morphT <= 0f || morphT >= 1f)
            return;

        float intensity = 4f * morphT * (1f - morphT);
        int bands = (int)(intensity * 14f);
        for (int i = 0; i < bands; i++)
        {
            int y = i * 67 % h;
            int bh = 2 + i * 11 % 10;
            float a = 0.05f + intensity * 0.15f;
            renderer.FillRect(0, y, w, bh, new Vector4(0.2f, 0.8f, 1f, a));
        }
    }

    private void DrawLabel(Dx12Renderer renderer, int w, int h, float alpha)
    {
        string label = _morphT < 0.5f ? "FEMALE FACE" : "ROBOT FACE";
        float fade = _morphT < 0.5f ? Math.Clamp(_time / 0.8f, 0f, 1f) : Math.Clamp((_morphT - 0.5f) * 4f, 0f, 1f);
        Vector4 c = new(0.8f, 0.85f, 1f, fade * alpha * 0.9f);
        var m = renderer.MeasureText(label, 28f);
        renderer.DrawText(label, (w - m.Width) * 0.5f, h - 68f, 28f, c);
    }

    private static void DrawScanlines(Dx12Renderer renderer, int w, int h)
    {
        Vector4 c = new(0f, 0f, 0f, 0.2f);
        for (int y = 0; y < h; y += 2)
            renderer.FillRect(0, y, w, 1, c);
    }

    public void Dispose()
    {
    }
}
