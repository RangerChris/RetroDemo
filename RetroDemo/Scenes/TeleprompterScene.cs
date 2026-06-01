using System.Numerics;

namespace RetroDemo.Scenes;

public sealed class TeleprompterScene : IScene
{
    private static readonly string[] Messages =
    [
        "WELCOME TO RETRO DEMO",
        "DIRECTX 12 MIGRATION COMPLETE",
    ];

    private const float TypeSpeed = 18f;
    private const float HoldDuration = 2.3f;
    private const float FadeDuration = 0.7f;
    private const int CopperHeight = 52;

    private enum Phase { FadeIn, Typing, Hold, FadeOut, Done }

    private Phase _phase = Phase.FadeIn;
    private int _msgIndex;
    private float _phaseTime;
    private float _totalTime;
    private float _alpha;
    private int _visibleChars;

    public TeleprompterScene(int screenWidth, int screenHeight)
    {
        _ = screenWidth;
        _ = screenHeight;
    }

    public bool Update(float deltaTime)
    {
        _totalTime += deltaTime;
        _phaseTime += deltaTime;

        switch (_phase)
        {
            case Phase.FadeIn:
                _alpha = Math.Min(_phaseTime / FadeDuration, 1f);
                if (_phaseTime >= FadeDuration)
                {
                    _phase = Phase.Typing;
                    _phaseTime = 0f;
                    _visibleChars = 0;
                }
                break;

            case Phase.Typing:
                _alpha = 1f;
                _visibleChars = (int)(_phaseTime * TypeSpeed);
                int maxChars = Messages[_msgIndex].Length;
                if (_visibleChars >= maxChars)
                {
                    _visibleChars = maxChars;
                    _phase = Phase.Hold;
                    _phaseTime = 0f;
                }
                break;

            case Phase.Hold:
                _alpha = 1f;
                if (_phaseTime >= HoldDuration)
                {
                    _phase = Phase.FadeOut;
                    _phaseTime = 0f;
                }
                break;

            case Phase.FadeOut:
                _alpha = 1f - Math.Min(_phaseTime / FadeDuration, 1f);
                if (_phaseTime >= FadeDuration)
                {
                    _msgIndex++;
                    if (_msgIndex >= Messages.Length)
                    {
                        _phase = Phase.Done;
                    }
                    else
                    {
                        _phase = Phase.FadeIn;
                        _phaseTime = 0f;
                        _visibleChars = 0;
                    }
                }
                break;

            case Phase.Done:
                return true;
        }

        return false;
    }

    public void Draw(Dx12Renderer renderer)
    {
        int w = renderer.Width;
        int h = renderer.Height;

        renderer.Clear(new Vector4(0.02f, 0.02f, 0.035f, 1f));
        renderer.BeginOverlay();

        DrawCopperBars(renderer, 0, CopperHeight, w);
        DrawCopperBars(renderer, h - CopperHeight, CopperHeight, w);

        if (_phase != Phase.Done)
        {
            string msg = Messages[_msgIndex];
            string shown = msg[..Math.Clamp(_visibleChars, 0, msg.Length)];

            float fontSize = Math.Max(22f, w / 24f);
            var size = renderer.MeasureText(shown, fontSize);
            float textX = (w - size.Width) * 0.5f;
            float textY = (h - fontSize) * 0.5f;

            Vector4 glow = new(1f, 0.6f, 0.2f, _alpha * 0.35f);
            Vector4 main = new(1f, 0.85f, 0.26f, _alpha);

            for (int d = 4; d >= 1; d--)
            {
                renderer.DrawText(shown, textX - d, textY + d, fontSize, glow);
                renderer.DrawText(shown, textX + d, textY + d, fontSize, glow);
            }

            renderer.DrawText(shown, textX, textY, fontSize, main);

            if (_phase == Phase.Typing && ((int)(_totalTime * 2f) % 2 == 0))
            {
                float cursorX = textX + size.Width + 6f;
                renderer.FillRect(cursorX, textY, fontSize * 0.45f, fontSize, main);
            }

            float topLineY = textY - 20f;
            float bottomLineY = textY + fontSize + 14f;
            Vector4 lineColor = new(1f, 0.65f, 0.2f, _alpha * 0.8f);
            renderer.DrawLine(textX, topLineY, textX + size.Width, topLineY, 2f, lineColor);
            renderer.DrawLine(textX, bottomLineY, textX + size.Width, bottomLineY, 2f, lineColor);
        }

        DrawScanlines(renderer, w, h);
        renderer.EndOverlay();
    }

    private void DrawCopperBars(Dx12Renderer renderer, int startY, int height, int width)
    {
        for (int row = 0; row < height; row++)
        {
            float t = _totalTime;
            float hue = ((row * 360f / height) + t * 85f) % 360f;
            Vector4 c = HsvToRgb(hue, 1f, 0.55f + 0.45f * MathF.Sin(row * MathF.PI / height));
            renderer.FillRect(0, startY + row, width, 1, c);
        }

        Vector4 edge = new(1f, 1f, 1f, 0.5f);
        renderer.FillRect(0, startY, width, 2, edge);
        renderer.FillRect(0, startY + height - 2, width, 2, edge);
    }

    private static void DrawScanlines(Dx12Renderer renderer, int w, int h)
    {
        Vector4 scan = new(0f, 0f, 0f, 0.23f);
        for (int y = 0; y < h; y += 2)
            renderer.FillRect(0, y, w, 1, scan);
    }

    private static Vector4 HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - MathF.Abs((h / 60f % 2) - 1));
        float m = v - c;

        float r = 0, g = 0, b = 0;

        if (h < 60f) { r = c; g = x; }
        else if (h < 120f) { r = x; g = c; }
        else if (h < 180f) { g = c; b = x; }
        else if (h < 240f) { g = x; b = c; }
        else if (h < 300f) { r = x; b = c; }
        else { r = c; b = x; }

        return new Vector4(r + m, g + m, b + m, 1f);
    }

    public void Dispose()
    {
    }
}
