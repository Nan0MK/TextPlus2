using System;
using Avalonia.Media;

namespace ChiselUI.Avalonia;

/// <summary>Colour math that mirrors the CSS the web build uses (color-mix in sRGB, premultiplied alpha).</summary>
public static class ChiselColor
{
    public static Color Hex(string hex) => Color.Parse(hex);

    public static Color Rgba(byte r, byte g, byte b, double a) => Color.FromArgb(ToByte(a * 255), r, g, b);

    public static Color WithAlpha(Color c, double a) => Color.FromArgb(ToByte(a * 255), c.R, c.G, c.B);

    public static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);
    public static readonly Color White = Colors.White;
    public static readonly Color Black = Colors.Black;

    /// <summary>color-mix(in srgb, a p, b) with premultiplied alpha, p in 0..1.</summary>
    public static Color Mix(Color a, double p, Color b)
    {
        double q = 1 - p;
        double aa = a.A / 255.0, ba = b.A / 255.0;
        double alpha = aa * p + ba * q;
        if (alpha <= 0.0001) return Transparent;
        double r = (a.R * aa * p + b.R * ba * q) / alpha;
        double g = (a.G * aa * p + b.G * ba * q) / alpha;
        double bl = (a.B * aa * p + b.B * ba * q) / alpha;
        return Color.FromArgb(ToByte(alpha * 255), ToByte(r), ToByte(g), ToByte(bl));
    }

    /// <summary>Source-over composite of <paramref name="top"/> onto <paramref name="bottom"/>.</summary>
    public static Color Over(Color top, Color bottom)
    {
        double ta = top.A / 255.0, ba = bottom.A / 255.0;
        double oa = ta + ba * (1 - ta);
        if (oa <= 0.0001) return Transparent;
        double C(byte t, byte b) => (t * ta + b * ba * (1 - ta)) / oa;
        return Color.FromArgb(ToByte(oa * 255), ToByte(C(top.R, bottom.R)), ToByte(C(top.G, bottom.G)), ToByte(C(top.B, bottom.B)));
    }

    public static Color Lerp(Color a, Color b, double t) => Mix(b, t, a);

    static byte ToByte(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);
}
