using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ChiselUI.Avalonia.Controls;

/// <summary>
/// The ChiselUI "desktop": the aurora wallpaper from the web build (diagonal base gradient, four soft
/// elliptical glows, two light ribbons) drawn with plain gradients. It is a Decorator: put your desktop content in Child. No blur, no bitmaps, renders once per size.
/// </summary>
public class AuroraBackground : Decorator
{
    public static readonly StyledProperty<Color> BaseAProperty = AvaloniaProperty.Register<AuroraBackground, Color>(nameof(BaseA), Color.Parse("#2a5ab0"));
    public static readonly StyledProperty<Color> BaseBProperty = AvaloniaProperty.Register<AuroraBackground, Color>(nameof(BaseB), Color.Parse("#14294c"));
    public static readonly StyledProperty<Color> Aurora1Property = AvaloniaProperty.Register<AuroraBackground, Color>(nameof(Aurora1), Colors.Teal);
    public static readonly StyledProperty<Color> Aurora2Property = AvaloniaProperty.Register<AuroraBackground, Color>(nameof(Aurora2), Colors.MediumPurple);
    public static readonly StyledProperty<Color> Aurora3Property = AvaloniaProperty.Register<AuroraBackground, Color>(nameof(Aurora3), Color.FromArgb(115, 255, 255, 255));

    public Color BaseA { get => GetValue(BaseAProperty); set => SetValue(BaseAProperty, value); }
    public Color BaseB { get => GetValue(BaseBProperty); set => SetValue(BaseBProperty, value); }
    public Color Aurora1 { get => GetValue(Aurora1Property); set => SetValue(Aurora1Property, value); }
    public Color Aurora2 { get => GetValue(Aurora2Property); set => SetValue(Aurora2Property, value); }
    public Color Aurora3 { get => GetValue(Aurora3Property); set => SetValue(Aurora3Property, value); }

    static AuroraBackground()
    {
        AffectsRender<AuroraBackground>(BaseAProperty, BaseBProperty, Aurora1Property, Aurora2Property, Aurora3Property);
        ClipToBoundsProperty.OverrideDefaultValue<AuroraBackground>(true);
    }

    static (RelativePoint, RelativePoint) Angle(double deg, Size s)
    {
        double r = deg * Math.PI / 180, dx = Math.Sin(r), dy = -Math.Cos(r);
        double len = Math.Abs(s.Width * dx) + Math.Abs(s.Height * dy);
        double cx = s.Width / 2, cy = s.Height / 2;
        return (new RelativePoint(cx - dx * len / 2, cy - dy * len / 2, RelativeUnit.Absolute),
                new RelativePoint(cx + dx * len / 2, cy + dy * len / 2, RelativeUnit.Absolute));
    }

    static IBrush Linear(double deg, Size s, params (double, Color)[] stops)
    {
        var (a, b) = Angle(deg, s);
        var gs = new ImmutableGradientStop[stops.Length];
        for (int i = 0; i < stops.Length; i++) gs[i] = new ImmutableGradientStop(stops[i].Item1, stops[i].Item2);
        return new ImmutableLinearGradientBrush(gs, 1, null, null, GradientSpreadMethod.Pad, a, b);
    }

    static void Glow(DrawingContext ctx, Size s, double rx, double ry, double px, double py, Color c, double fade)
    {
        var center = new Point(s.Width * px, s.Height * py);
        var rect = new Rect(center.X - rx, center.Y - ry, rx * 2, ry * 2);
        var t = Color.FromArgb(0, c.R, c.G, c.B);
        var brush = new ImmutableRadialGradientBrush(new[] {
                new ImmutableGradientStop(0, c), new ImmutableGradientStop(fade * .5, ChiselColor.Mix(c, .55, t)), new ImmutableGradientStop(fade, t) },
            1, null, null, GradientSpreadMethod.Pad, RelativePoint.Center, RelativePoint.Center, new RelativeScalar(.5, RelativeUnit.Relative));
        ctx.DrawRectangle(brush, null, rect);
    }

    public override void Render(DrawingContext ctx)
    {
        var s = Bounds.Size;
        if (s.Width <= 0 || s.Height <= 0) return;
        var full = new Rect(s);
        var t = Colors.Transparent;
        ctx.DrawRectangle(Linear(162, s, (0, BaseA), (1, BaseB)), null, full);
        ctx.DrawRectangle(Linear(128, s, (.58, t), (.63, Color.FromArgb(15, 255, 255, 255)), (.69, t)), null, full);
        ctx.DrawRectangle(Linear(118, s, (.36, t), (.44, Color.FromArgb(26, 255, 255, 255)), (.48, Color.FromArgb(8, 255, 255, 255)), (.56, t)), null, full);
        Glow(ctx, s, 700, 420, .70, .96, ChiselColor.Mix(Aurora2, .55, t), .70);
        Glow(ctx, s, 1100, 760, .14, .82, ChiselColor.Mix(Aurora1, .75, t), .68);
        Glow(ctx, s, 1000, 640, .96, .12, ChiselColor.Mix(Aurora2, .85, t), .66);
        Glow(ctx, s, 900, 520, .08, -.06, Aurora3, .70);
        base.Render(ctx);
    }
}
