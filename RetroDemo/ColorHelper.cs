using Raylib_cs;

namespace RetroDemo;

/// <summary>
/// Provides an unambiguous Color factory, since Raylib-cs 8 exposes both
/// Color(byte,byte,byte,byte) and Color(int,int,int,int) constructors.
/// Using int parameters here lets the compiler select the int overload.
/// </summary>
internal static class ColorHelper
{
    /// <summary>Create a Color from integer RGBA components (0-255 each).</summary>
    internal static Color Rgba(int r, int g, int b, int a = 255) =>
        new Color(r, g, b, a);

    /// <summary>Set the alpha of an existing colour.</summary>
    internal static Color WithAlpha(Color c, int a) =>
        new Color(c.R, c.G, c.B, a);
}
