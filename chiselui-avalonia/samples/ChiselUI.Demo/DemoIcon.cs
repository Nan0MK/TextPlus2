using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ChiselUI.Demo;

/// <summary>
/// Small vector icons for the demo (folders, documents, drives, toolbar glyphs), drawn with plain gradients in the
/// 2000s desktop style. Scales to any Size. Only for the demo; the library itself ships no icon set.
/// </summary>
public class DemoIcon : Control
{
    public static readonly StyledProperty<string> KindProperty = AvaloniaProperty.Register<DemoIcon, string>(nameof(Kind), "file");
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<DemoIcon, double>(nameof(Size), 16);
    public string Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    static DemoIcon() { AffectsRender<DemoIcon>(KindProperty, SizeProperty); AffectsMeasure<DemoIcon>(SizeProperty); }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    static IBrush V(string a, string b) => new ImmutableLinearGradientBrush(new[] { new ImmutableGradientStop(0, Color.Parse(a)), new ImmutableGradientStop(1, Color.Parse(b)) },
        1, null, null, GradientSpreadMethod.Pad, new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(0, 1, RelativeUnit.Relative));
    static IBrush S(string a) => new ImmutableSolidColorBrush(Color.Parse(a));
    static IPen P(string c, double w) => new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse(c)), w, null, PenLineCap.Round, PenLineJoin.Round);

    public override void Render(DrawingContext ctx)
    {
        double k = Size / 16.0;
        using var _ = ctx.PushTransform(Matrix.CreateScale(k, k));
        switch (Kind)
        {
            case "folder": case "folder-code": case "folder-photo": case "folder-music": case "folder-doc": case "folder-zip": Folder(ctx, Kind); break;
            case "file": case "cpp": case "h": case "word": case "xls": Page(ctx, Kind); break;
            case "photo": Photo(ctx, "#3d8be0", "#d8eefc"); break;
            case "sunset": Photo(ctx, "#5b3c8e", "#f39c6b"); break;
            case "cd":
                ctx.DrawEllipse(V("#f4f6fa", "#b8c0cc"), P("#8a94a3", .5), new Point(8, 8), 7, 7);
                ctx.DrawEllipse(V("#e9b3ff", "#7ed6ff"), null, new Point(8, 8), 4.5, 4.5);
                ctx.DrawEllipse(S("#ffffff"), P("#8a94a3", .4), new Point(8, 8), 1.6, 1.6);
                ctx.DrawEllipse(V("#ff6aa8", "#d01f6a"), null, new Point(12.5, 12.5), 3.2, 3.2);
                ctx.DrawGeometry(null, P("#ffffff", .9), Geometry.Parse("M12,14 L12,11 L14,10.6"));
                break;
            case "drive": case "dvd":
                ctx.DrawRectangle(V("#eef1f5", "#9aa4b2"), P("#6b7686", .5), new RoundedRect(new Rect(1.5, 5, 13, 7), 1.5));
                ctx.DrawRectangle(V("#c7ced8", "#7d8796"), null, new Rect(2.5, 9.5, 11, 1.6));
                ctx.DrawEllipse(Kind == "dvd" ? S("#f0b030") : S("#3fd078"), null, new Point(12.5, 7.3), .9, .9);
                break;
            case "drive-big":
                ctx.DrawRectangle(V("#f2f4f8", "#a2acb9"), P("#6b7686", .4), new RoundedRect(new Rect(1, 3.5, 14, 8), 1.5));
                ctx.DrawRectangle(V("#cfd5de", "#8a95a5"), null, new Rect(2, 8.5, 12, 2));
                ctx.DrawRectangle(V("#4ea3ff", "#1f5fd0"), null, new RoundedRect(new Rect(2, 12.8, 9, 1.2), .6));
                ctx.DrawEllipse(S("#3fd078"), null, new Point(13, 6), .8, .8);
                break;
            case "computer":
                ctx.DrawRectangle(V("#5a6a80", "#2a3646"), null, new RoundedRect(new Rect(1.5, 2.5, 13, 9), 1));
                ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), null, new Rect(2.5, 3.5, 11, 7));
                ctx.DrawRectangle(S("#8a94a3"), null, new Rect(6, 11.5, 4, 1.6));
                ctx.DrawRectangle(V("#c7ced8", "#7d8796"), null, new RoundedRect(new Rect(4, 13, 8, 1.5), .7));
                break;
            case "network":
                ctx.DrawEllipse(V("#8fd0ff", "#1f6fd0"), P("#1a4f9a", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#e6f4ff", .7), Geometry.Parse("M1.5,8 L14.5,8 M8,1.5 C4.5,5 4.5,11 8,14.5 M8,1.5 C11.5,5 11.5,11 8,14.5"));
                break;
            case "star":
                ctx.DrawGeometry(V("#ffe27a", "#f0a020"), P("#c07a10", .5), Geometry.Parse("M8,1.2 L10,6 L15,6.3 L11.1,9.5 L12.4,14.5 L8,11.7 L3.6,14.5 L4.9,9.5 L1,6.3 L6,6 Z"));
                break;
            case "star-off":
                ctx.DrawGeometry(S("#8a94a366"), null, Geometry.Parse("M8,1.2 L10,6 L15,6.3 L11.1,9.5 L12.4,14.5 L8,11.7 L3.6,14.5 L4.9,9.5 L1,6.3 L6,6 Z"));
                break;
            case "library":
                ctx.DrawRectangle(V("#ffd970", "#e0a020"), null, new Rect(1.5, 3, 3, 11));
                ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), null, new Rect(5, 2, 3, 12));
                ctx.DrawRectangle(V("#9be37a", "#3a9a2c"), null, new Rect(8.5, 4, 2.6, 10));
                ctx.DrawRectangle(V("#ff9a7a", "#c83a2a"), null, new Rect(11.5, 3, 3, 11));
                break;
            case "desktop":
                ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), P("#1a4f9a", .5), new RoundedRect(new Rect(1.5, 3, 13, 9), 1));
                ctx.DrawRectangle(S("#6b7686"), null, new Rect(5, 12.5, 6, 1.5));
                break;
            case "download":
                ctx.DrawRectangle(V("#ffe08a", "#e8a828"), null, new RoundedRect(new Rect(1.5, 4, 13, 10), 1.2));
                ctx.DrawGeometry(V("#7cc4ff", "#1f5fd0"), null, Geometry.Parse("M6.5,2 L9.5,2 L9.5,7 L12,7 L8,11.5 L4,7 L6.5,7 Z"));
                break;
            case "recent":
                ctx.DrawEllipse(V("#ffffff", "#c9d2de"), P("#6b7686", .7), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#2a3646", 1.2), Geometry.Parse("M8,4 L8,8 L11,9.5"));
                break;
            case "picture": Photo(ctx, "#3d8be0", "#d8eefc"); break;
            case "video":
                ctx.DrawRectangle(V("#5a6a80", "#2a3646"), null, new RoundedRect(new Rect(1.5, 3, 13, 10), 1));
                for (int i = 0; i < 4; i++) { ctx.DrawRectangle(S("#c9d2de"), null, new Rect(2.5 + i * 3.2, 3.8, 1.6, 1.2)); ctx.DrawRectangle(S("#c9d2de"), null, new Rect(2.5 + i * 3.2, 11, 1.6, 1.2)); }
                break;
            case "music":
                ctx.DrawGeometry(V("#ff8ac0", "#c01f6a"), null, Geometry.Parse("M6,3 L14,1.5 L14,11 A2.3,2 0 1 1 12.5,9.2 L12.5,4.3 L7.5,5.3 L7.5,12.5 A2.3,2 0 1 1 6,10.7 Z"));
                break;
            case "workspace":
                ctx.DrawEllipse(V("#8fd0ff", "#1f6fd0"), P("#1a4f9a", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#e6f4ff", .7), Geometry.Parse("M1.5,8 L14.5,8 M8,1.5 C4.5,5 4.5,11 8,14.5 M8,1.5 C11.5,5 11.5,11 8,14.5"));
                break;
            case "project":
                ctx.DrawRectangle(V("#ff9a7a", "#d0453a"), null, new Rect(1.5, 1.5, 6, 6));
                ctx.DrawRectangle(V("#9be37a", "#3a9a2c"), null, new Rect(8.5, 1.5, 6, 6));
                ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), null, new Rect(1.5, 8.5, 6, 6));
                ctx.DrawRectangle(V("#ffe27a", "#e0a020"), null, new Rect(8.5, 8.5, 6, 6));
                break;
            // ---- toolbar glyphs ----
            case "new": Page(ctx, "file"); ctx.DrawGeometry(S("#f0b030"), null, Geometry.Parse("M12,1 L13,3 L15,4 L13,5 L12,7 L11,5 L9,4 L11,3 Z")); break;
            case "open": Folder(ctx, "folder"); break;
            case "save":
                ctx.DrawRectangle(V("#5b8ee6", "#1f4fa8"), P("#163d80", .5), new RoundedRect(new Rect(1.5, 1.5, 13, 13), 1.5));
                ctx.DrawRectangle(S("#f4f6fa"), null, new Rect(4, 2, 8, 5));
                ctx.DrawRectangle(S("#2a3646"), null, new Rect(5, 10, 6, 4));
                break;
            case "undo": ctx.DrawGeometry(null, P("#2f9a52", 2), Geometry.Parse("M4,5 L11,5 A3.5,3.5 0 0 1 11,12 L6,12 M6.5,2 L3.5,5 L6.5,8")); break;
            case "redo": ctx.DrawGeometry(null, P("#8a94a3", 2), Geometry.Parse("M12,5 L5,5 A3.5,3.5 0 0 0 5,12 L10,12 M9.5,2 L12.5,5 L9.5,8")); break;
            case "cut":
                ctx.DrawGeometry(null, P("#c83a2a", 1.3), Geometry.Parse("M5,1.5 L10.5,10 M11,1.5 L5.5,10"));
                ctx.DrawEllipse(null, P("#c83a2a", 1.3), new Point(4.5, 12), 2.2, 2.2); ctx.DrawEllipse(null, P("#c83a2a", 1.3), new Point(11.5, 12), 2.2, 2.2);
                break;
            case "copy":
                ctx.DrawRectangle(V("#ffffff", "#dde3ec"), P("#6b7686", .8), new Rect(1.5, 1.5, 8.5, 10));
                ctx.DrawRectangle(V("#ffffff", "#dde3ec"), P("#6b7686", .8), new Rect(6, 4.5, 8.5, 10));
                break;
            case "paste":
                ctx.DrawRectangle(V("#e8b870", "#a87228"), P("#7a5018", .6), new RoundedRect(new Rect(2, 2.5, 11, 12.5), 1));
                ctx.DrawRectangle(V("#ffffff", "#dde3ec"), P("#6b7686", .6), new Rect(6, 6, 8.5, 9));
                ctx.DrawRectangle(S("#6b7686"), null, new RoundedRect(new Rect(5, 1, 5, 3), 1));
                break;
            case "search":
                ctx.DrawEllipse(V("#e6f4ff", "#9cc8f0"), P("#2f6bd8", 1.6), new Point(6.5, 6.5), 4.5, 4.5);
                ctx.DrawGeometry(null, P("#2a3646", 2.2), Geometry.Parse("M10,10 L14,14"));
                break;
            case "gear":
                ctx.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#6b7686")), 3, new ImmutableDashStyle(new[] { .8, .8 }, 0)), new Point(8, 8), 5.5, 5.5);
                ctx.DrawEllipse(V("#c7ced8", "#6b7686"), null, new Point(8, 8), 4.5, 4.5);
                ctx.DrawEllipse(S("#f4f6fa"), null, new Point(8, 8), 1.8, 1.8);
                break;
            case "play": ctx.DrawGeometry(V("#7fe07a", "#2a9a3a"), P("#1f7a2c", .5), Geometry.Parse("M3.5,1.5 L14,8 L3.5,14.5 Z")); break;
            case "buildrun":
                ctx.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#6b7686")), 2.4, new ImmutableDashStyle(new[] { .8, .8 }, 0)), new Point(6, 6), 3.8, 3.8);
                ctx.DrawGeometry(V("#7fe07a", "#2a9a3a"), null, Geometry.Parse("M8,7 L15,11 L8,15 Z"));
                break;
            case "stop":
                ctx.DrawEllipse(V("#ff7a6a", "#c0261a"), P("#8a1a10", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#ffffff", 1.8), Geometry.Parse("M5.5,5.5 L10.5,10.5 M10.5,5.5 L5.5,10.5"));
                break;
            case "bug":
                ctx.DrawEllipse(V("#ff7a6a", "#a8231a"), null, new Point(8, 9.5), 4.5, 5);
                ctx.DrawEllipse(S("#2a3646"), null, new Point(8, 4.5), 2.6, 2.2);
                ctx.DrawGeometry(null, P("#2a3646", 1), Geometry.Parse("M3.5,7 L1.5,5.5 M3.2,10 L1,10 M3.8,13 L2,14.5 M12.5,7 L14.5,5.5 M12.8,10 L15,10 M12.2,13 L14,14.5 M8,5 L8,14.5"));
                break;
            case "step": ctx.DrawGeometry(null, P("#2f6bd8", 2), Geometry.Parse("M2,11 C2,4 12,3 12.5,9 M10,7 L12.8,10 L15,6.8")); break;
            case "bookmark": ctx.DrawGeometry(V("#7cc4ff", "#1f5fd0"), null, Geometry.Parse("M4,1.5 L12,1.5 L12,14.5 L8,11 L4,14.5 Z")); break;
            case "info":
                ctx.DrawEllipse(V("#7cc4ff", "#1f5fd0"), P("#163d80", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#ffffff", 1.8), Geometry.Parse("M8,7 L8,11.5")); ctx.DrawEllipse(S("#ffffff"), null, new Point(8, 4.6), 1.1, 1.1);
                break;
            case "warn":
                ctx.DrawGeometry(V("#ffe27a", "#e8a020"), P("#a86a10", .6), Geometry.Parse("M8,1.5 L15,14 L1,14 Z"));
                ctx.DrawGeometry(null, P("#2a1a00", 1.6), Geometry.Parse("M8,6 L8,10")); ctx.DrawEllipse(S("#2a1a00"), null, new Point(8, 12), .9, .9);
                break;
            case "error":
                ctx.DrawEllipse(V("#ff7a6a", "#c0261a"), P("#8a1a10", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#ffffff", 1.8), Geometry.Parse("M5.5,5.5 L10.5,10.5 M10.5,5.5 L5.5,10.5"));
                break;
            case "ok":
                ctx.DrawEllipse(V("#7fe07a", "#2a9a3a"), P("#1f7a2c", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#ffffff", 1.8), Geometry.Parse("M4.8,8.2 L7.2,10.5 L11.2,5.6"));
                break;
            case "log":
                ctx.DrawRectangle(V("#ffffff", "#dde3ec"), P("#6b7686", .7), new Rect(2.5, 1.5, 11, 13));
                ctx.DrawGeometry(null, P("#6b7686", .9), Geometry.Parse("M4.5,5 L11.5,5 M4.5,8 L11.5,8 M4.5,11 L9,11"));
                break;
            case "slideshow":
                ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), P("#1a4f9a", .5), new RoundedRect(new Rect(1.5, 2.5, 13, 9.5), 1));
                ctx.DrawGeometry(S("#ffffff"), null, Geometry.Parse("M6.5,4.8 L10.5,7.2 L6.5,9.6 Z"));
                break;
            case "share": ctx.DrawGeometry(null, P("#2a9a3a", 1.6), Geometry.Parse("M4,8 L12,3.5 M4,8 L12,12.5")); foreach (var p in new[] { new Point(4, 8), new Point(12, 3.5), new Point(12, 12.5) }) ctx.DrawEllipse(V("#7fe07a", "#2a9a3a"), null, p, 2.4, 2.4); break;
            case "burn":
                ctx.DrawEllipse(V("#f4f6fa", "#b8c0cc"), P("#8a94a3", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawEllipse(V("#ffd970", "#ff8a3a"), null, new Point(8, 8), 3.8, 3.8);
                ctx.DrawEllipse(S("#ffffff"), null, new Point(8, 8), 1.3, 1.3);
                break;
            case "newfolder": Folder(ctx, "folder"); ctx.DrawGeometry(S("#3a9a2c"), null, Geometry.Parse("M12,0.5 L13,3 L15.5,4 L13,5 L12,7.5 L11,5 L8.5,4 L11,3 Z")); break;
            case "organize": ctx.DrawRectangle(V("#c7ced8", "#7d8796"), null, new RoundedRect(new Rect(2, 2, 12, 12), 1.5)); ctx.DrawRectangle(S("#ffffff"), null, new Rect(4, 4, 8, 3)); ctx.DrawRectangle(S("#ffffff"), null, new Rect(4, 9, 8, 3)); break;
            case "views":
                foreach (var (x, y) in new[] { (2, 2), (9, 2), (2, 9), (9, 9) }) ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), null, new Rect(x, y, 5, 5));
                break;
            case "help":
                ctx.DrawEllipse(V("#7cc4ff", "#1f5fd0"), P("#163d80", .5), new Point(8, 8), 6.5, 6.5);
                ctx.DrawGeometry(null, P("#ffffff", 1.6), Geometry.Parse("M5.8,6 A2.3,2.2 0 1 1 8,8.2 L8,9.6")); ctx.DrawEllipse(S("#ffffff"), null, new Point(8, 11.8), 1, 1);
                break;
            case "refresh": ctx.DrawGeometry(null, P("#3fd078", 1.6), Geometry.Parse("M13,8 A5,5 0 1 1 11.5,4.4 M12,1.5 L12,5 L8.5,5")); break;
            case "copyjob":
                Folder(ctx, "folder");
                break;
            default: Page(ctx, "file"); break;
        }
    }

    static void Folder(DrawingContext ctx, string kind)
    {
        ctx.DrawGeometry(V("#f6c044", "#d9961a"), null, Geometry.Parse("M1,3.5 Q1,2.5 2,2.5 L6,2.5 L7.3,4 L14,4 Q15,4 15,5 L15,6 L1,6 Z"));
        ctx.DrawRectangle(V("#fff4cc", "#f1d58a"), null, new Rect(2, 4.6, 12, 4));
        ctx.DrawGeometry(V("#ffe08a", "#f0b33a"), new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#c98a18")), .4), Geometry.Parse("M0.8,6.6 Q0.8,5.6 1.8,5.6 L14.2,5.6 Q15.2,5.6 15.2,6.6 L14.6,13.6 Q14.5,14.5 13.6,14.5 L2.4,14.5 Q1.5,14.5 1.4,13.6 Z"));
        ctx.DrawGeometry(null, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(150, 255, 255, 255)), .5), Geometry.Parse("M1.6,6.5 L14.4,6.5"));
        var g = new ImmutableSolidColorBrush(Color.FromArgb(200, 255, 255, 255));
        var pen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#b07810")), .8, null, PenLineCap.Round, PenLineJoin.Round);
        switch (kind)
        {
            case "folder-code": ctx.DrawGeometry(null, pen, Geometry.Parse("M6.5,8.5 L5,10 L6.5,11.5 M9.5,8.5 L11,10 L9.5,11.5 M8.6,8 L7.4,12")); break;
            case "folder-photo": ctx.DrawGeometry(new ImmutableSolidColorBrush(Color.Parse("#b07810")), null, Geometry.Parse("M5,12 L7,9 L8.5,11 L9.5,10 L11,12 Z")); break;
            case "folder-music": ctx.DrawGeometry(null, pen, Geometry.Parse("M7,12 L7,8.5 L10,8 L10,11.5")); break;
            case "folder-doc": ctx.DrawGeometry(null, pen, Geometry.Parse("M5.5,9 L10.5,9 M5.5,10.5 L10.5,10.5 M5.5,12 L9,12")); break;
            case "folder-zip": ctx.DrawRectangle(V("#c7ced8", "#7d8796"), null, new Rect(7.3, 5.6, 1.4, 6)); ctx.DrawRectangle(S("#6b7686"), null, new RoundedRect(new Rect(6.8, 11, 2.4, 2.6), .6)); break;
        }
    }

    static void Page(DrawingContext ctx, string kind)
    {
        ctx.DrawGeometry(V("#ffffff", "#e3e8ef"), new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#8a94a3")), .6), Geometry.Parse("M3,1 L10,1 L13.5,4.5 L13.5,15 L3,15 Z"));
        ctx.DrawGeometry(V("#f4f6fa", "#c9d2de"), new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#8a94a3")), .5), Geometry.Parse("M10,1 L10,4.5 L13.5,4.5"));
        switch (kind)
        {
            case "cpp": ctx.DrawRectangle(V("#7cc4ff", "#2f6bd8"), null, new RoundedRect(new Rect(1, 8, 9, 6), 1)); ctx.DrawGeometry(null, P("#ffffff", .9), Geometry.Parse("M4.5,9.5 L3,11 L4.5,12.5 M6.5,9.5 L8,11 L6.5,12.5")); break;
            case "h": ctx.DrawRectangle(V("#c7a4ff", "#7a4fd8"), null, new RoundedRect(new Rect(1, 8, 9, 6), 1)); ctx.DrawGeometry(null, P("#ffffff", 1), Geometry.Parse("M3.6,9.3 L3.6,12.7 M6.4,9.3 L6.4,12.7 M3.6,11 L6.4,11")); break;
            case "word": ctx.DrawRectangle(V("#5b8ee6", "#1f4fa8"), null, new RoundedRect(new Rect(1, 8, 8, 6.5), 1)); ctx.DrawGeometry(null, P("#ffffff", .9), Geometry.Parse("M2.5,9.5 L3.6,13 L5,10.5 L6.4,13 L7.5,9.5")); break;
            case "xls": ctx.DrawRectangle(V("#6fd07a", "#1f8a3a"), null, new RoundedRect(new Rect(1, 8, 8, 6.5), 1)); ctx.DrawGeometry(null, P("#ffffff", .8), Geometry.Parse("M2.5,10 L7.5,10 M2.5,12 L7.5,12 M5,9 L5,13.5")); break;
            default: ctx.DrawGeometry(null, P("#a8b2c0", .7), Geometry.Parse("M5,6.5 L11.5,6.5 M5,8.5 L11.5,8.5 M5,10.5 L11.5,10.5 M5,12.5 L9,12.5")); break;
        }
    }

    static void Photo(DrawingContext ctx, string sky, string sky2)
    {
        ctx.DrawRectangle(S("#ffffff"), new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#8a94a3")), .5), new Rect(1, 2, 14, 12));
        ctx.DrawRectangle(V(sky2, sky), null, new Rect(2.2, 3.2, 11.6, 9.6));
        ctx.DrawGeometry(V("#e8eef6", "#4a6a8a"), null, Geometry.Parse("M2.2,12.8 L6,7 L8.5,10 L10.5,8 L13.8,12.8 Z"));
        ctx.DrawEllipse(S("#ffe27a"), null, new Point(11.5, 5.5), 1.3, 1.3);
    }
}
