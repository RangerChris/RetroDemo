using System.Numerics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using DrawingColor = System.Drawing.Color;

namespace RetroDemo;

public sealed class Dx12Renderer : IDisposable
{
    private readonly IntPtr _hwnd;

    private readonly Graphics _presentGraphics;
    private readonly Bitmap _frameBuffer;
    private readonly Graphics _frameGraphics;
    private readonly SolidBrush _sharedBrush;
    private readonly Pen _sharedPen;
    private readonly Dictionary<int, Font> _fontCache = [];

    private Graphics? _overlayGraphics;
    private Vector4 _clearColor = new(0f, 0f, 0f, 1f);

    public int Width { get; }
    public int Height { get; }

    public Dx12Renderer(IntPtr hwnd, int width, int height)
    {
        _hwnd = hwnd;
        Width = width;
        Height = height;

        _presentGraphics = Graphics.FromHwnd(_hwnd);
        _frameBuffer = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        _frameGraphics = Graphics.FromImage(_frameBuffer);
        _frameGraphics.SmoothingMode = SmoothingMode.None;
        _frameGraphics.CompositingQuality = CompositingQuality.HighSpeed;
        _frameGraphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        _sharedBrush = new SolidBrush(DrawingColor.Black);
        _sharedPen = new Pen(DrawingColor.Black, 1f);
    }

    public void Clear(Vector4 color)
    {
        _clearColor = color;
    }

    public void BeginOverlay()
    {
        _overlayGraphics = _frameGraphics;
        _overlayGraphics.SmoothingMode = SmoothingMode.None;
        _overlayGraphics.CompositingQuality = CompositingQuality.HighSpeed;
        _overlayGraphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        _overlayGraphics.Clear(ToColor(_clearColor));
    }

    public void EndOverlay()
    {
        _presentGraphics.DrawImageUnscaled(_frameBuffer, 0, 0);
        _overlayGraphics = null;
    }

    public void FillRect(float x, float y, float w, float h, Vector4 color)
    {
        if (_overlayGraphics is null)
            return;

        _sharedBrush.Color = ToColor(color);
        _overlayGraphics.FillRectangle(_sharedBrush, x, y, w, h);
    }

    public void FillEllipse(float x, float y, float w, float h, Vector4 color)
    {
        if (_overlayGraphics is null)
            return;

        _sharedBrush.Color = ToColor(color);
        _overlayGraphics.FillEllipse(_sharedBrush, x, y, w, h);
    }

    public void DrawEllipse(float x, float y, float w, float h, float thickness, Vector4 color)
    {
        if (_overlayGraphics is null)
            return;

        _sharedPen.Color = ToColor(color);
        _sharedPen.Width = thickness;
        _overlayGraphics.DrawEllipse(_sharedPen, x, y, w, h);
    }

    public void FillCircle(float cx, float cy, float radius, Vector4 color)
    {
        FillEllipse(cx - radius, cy - radius, radius * 2f, radius * 2f, color);
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Vector4 color)
    {
        if (_overlayGraphics is null)
            return;

        _sharedPen.Color = ToColor(color);
        _sharedPen.Width = thickness;
        _overlayGraphics.DrawLine(_sharedPen, x1, y1, x2, y2);
    }

    public void DrawText(string text, float x, float y, float size, Vector4 color)
    {
        if (_overlayGraphics is null || string.IsNullOrEmpty(text))
            return;

        Font font = GetFont(size);
        _sharedBrush.Color = ToColor(color);
        _overlayGraphics.DrawString(text, font, _sharedBrush, x, y);
    }

    public SizeF MeasureText(string text, float size)
    {
        Font font = GetFont(size);
        return _frameGraphics.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic);
    }

    private Font GetFont(float size)
    {
        int key = (int)Math.Clamp(MathF.Round(size), 8f, 128f);
        if (_fontCache.TryGetValue(key, out Font? font))
            return font;

        font = new Font("Consolas", key, FontStyle.Bold, GraphicsUnit.Pixel);
        _fontCache[key] = font;
        return font;
    }

    private static DrawingColor ToColor(Vector4 v)
    {
        static int C(float n) => (int)Math.Clamp(n * 255f, 0f, 255f);
        return DrawingColor.FromArgb(C(v.W), C(v.X), C(v.Y), C(v.Z));
    }

    public void Dispose()
    {
        _overlayGraphics = null;

        foreach (Font font in _fontCache.Values)
            font.Dispose();

        _sharedPen.Dispose();
        _sharedBrush.Dispose();
        _frameGraphics.Dispose();
        _frameBuffer.Dispose();
        _presentGraphics.Dispose();
    }
}
