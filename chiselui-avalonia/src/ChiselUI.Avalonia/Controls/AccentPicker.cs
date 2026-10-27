using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ChiselUI.Avalonia.Controls;

/// <summary>The seven gel accent swatches of the web build; clicking one calls <see cref="ChiselTheme.SetAccent"/>.</summary>
public class AccentPicker : StackPanel
{
    public AccentPicker()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 6;
        VerticalAlignment = VerticalAlignment.Center;
        foreach (var (name, color) in ChiselTheme.Swatches)
        {
            var b = new Button { Tag = color, Classes = { "swatch" } };
            ToolTip.SetTip(b, name);
            b.Resources["SwatchBrush"] = SwatchBrush(color);
            b.Resources["SwatchRing"] = new SolidColorBrush(ChiselColor.Mix(color, .55, Colors.Black));
            b.Resources["SwatchGlow"] = ChiselColor.Mix(color, .60, Colors.Transparent);
            b.Click += (_, _) => ChiselTheme.SetAccent(color);
            Children.Add(b);
        }
    }

    static IBrush SwatchBrush(Color sw)
    {
        var lo = ChiselColor.Mix(sw, .70, Colors.Black);
        var hi = ChiselColor.Mix(sw, .50, Colors.White);
        Color Body(double t) => ChiselColor.Lerp(sw, lo, t);
        var w = Colors.White;
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(ChiselColor.Over(ChiselColor.WithAlpha(w, .85), Body(0)), 0),
                new GradientStop(ChiselColor.Over(ChiselColor.WithAlpha(w, .55), Body(.06)), .06),
                new GradientStop(ChiselColor.Over(ChiselColor.WithAlpha(w, .22), Body(.475)), .475),
                new GradientStop(Body(.48), .48),
                new GradientStop(ChiselColor.Over(ChiselColor.WithAlpha(hi, .5), Body(1)), 1),
            }
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (ChiselTheme.Current is { } t) { t.AccentChanged += OnAccent; Sync(t.Accent); }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (ChiselTheme.Current is { } t) t.AccentChanged -= OnAccent;
    }

    void OnAccent(object? s, EventArgs e) { if (ChiselTheme.Current is { } t) Sync(t.Accent); }

    void Sync(Color accent)
    {
        foreach (var b in Children.OfType<Button>())
            b.Classes.Set("selected", b.Tag is Color c && c == accent);
    }
}
