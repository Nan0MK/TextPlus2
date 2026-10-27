using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ChiselUI.Avalonia;
using ChiselUI.Demo;
using SkiaSharp;

namespace ChiselUI.Shots;

/// <summary>Collects Avalonia log events (binding / resource / style problems) so the run fails on them.</summary>
sealed class CollectSink : ILogSink
{
    public readonly List<string> Events = new();
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Events.Add($"{level} [{area}] {messageTemplate} ({source?.GetType().Name})");
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        => Events.Add($"{level} [{area}] {messageTemplate} :: {string.Join(", ", propertyValues.Select(v => v?.ToString()))} ({source?.GetType().Name})");
}

static class Program
{
    static readonly CollectSink Sink = new();

    static int Main(string[] args)
    {
        string outDir = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "shots";
        bool quick = args.Contains("--quick");
        Directory.CreateDirectory(outDir);
        Logger.Sink = Sink;

        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseChiselUI()
            .SetupWithoutStarting();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Dispatcher.UIThread.Invoke(() =>
        {
            var win = new MainWindow { Width = 1440, Height = 900 };
            win.Show();
            Pump(400);
            Console.WriteLine($"first frame ready in {sw.ElapsedMilliseconds} ms");
            var accents = args.Contains("--accent") ? new[] { "#139a92" } : Array.Empty<string>();
            foreach (var (variant, tag) in new[] { (ThemeVariant.Light, "light"), (ThemeVariant.Dark, "dark") })
            {
                Application.Current!.RequestedThemeVariant = variant;
                win.ShowPage(false);
                Pump(400);
                if (!quick) Exercise(win);
                var scroller = win.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "PageScroller");
                scroller.Offset = default; Pump(150);
                Capture(win).Save(Path.Combine(outDir, $"showcase-{tag}.png"));

                // tall render for the full page + section crops
                win.Height = 2700; Pump(500);
                string full = Path.Combine(outDir, $"showcase-{tag}-full.png");
                Capture(win).Save(full);
                foreach (var name in new[] { "hero", "gallery", "buttons", "elevation" })
                    CropNamed(win, full, name, Path.Combine(outDir, $"zoom-{name}-{tag}.png"));
                win.Height = 900; Pump(200);

                // explorer page: icons, then details
                win.ShowPage(true); Pump(400);
                Capture(win).Save(Path.Combine(outDir, $"explorer-{tag}.png"));
                var ev = win.GetVisualDescendants().OfType<ExplorerView>().First();
                ev.ShowDetails(true); Pump(300);
                Capture(win).Save(Path.Combine(outDir, $"explorer-details-{tag}.png"));
                ev.ShowDetails(false);
                win.ShowPage(false); Pump(100);

                // the real-window explorer (ChiselWindow chrome on its own)
                var xw = new ExplorerWindow { Width = 1000, Height = 640 };
                xw.Show(); Pump(400);
                Capture(xw).Save(Path.Combine(outDir, $"explorer-window-{tag}.png"));
                xw.Close();
            }
            foreach (var a in accents)
            {
                ChiselTheme.SetAccent(global::Avalonia.Media.Color.Parse(a)); Pump(300);
                Application.Current!.RequestedThemeVariant = ThemeVariant.Light; Pump(300);
                Capture(win).Save(Path.Combine(outDir, $"showcase-accent-{a.TrimStart('#')}.png"));
            }
            win.Close();
        });

        var bad = Sink.Events.Distinct().ToList();
        Console.WriteLine($"Avalonia warnings/errors logged: {bad.Count}  (total {sw.ElapsedMilliseconds} ms)");
        foreach (var e in bad.Take(60)) Console.WriteLine("  " + e);
        Console.WriteLine(bad.Count == 0 ? "SMOKE OK" : "SMOKE FAILED");
        return bad.Count == 0 ? 0 : 1;
    }

    /// <summary>Drives hover / press / dropdown / toggle / tab states so every state style is instantiated under the log sink.</summary>
    static void Exercise(Window win)
    {
        foreach (var b in win.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible && b.IsEnabled).Take(14).ToList())
        {
            var p = b.TranslatePoint(new Point(b.Bounds.Width / 2, b.Bounds.Height / 2), win);
            if (p == null || p.Value.Y > 880 || p.Value.Y < 0 || b.Classes.Contains("swatch")) continue;
            if (b.Name is "PART_Close" or "PART_Minimize" or "PART_Maximize") continue;
            if (b.Content is string s && s.StartsWith("Open explorer")) continue;
            if (b.Name is "ThemeButton" or "TabShowcase" or "TabExplorer") continue;
            win.MouseMove(p.Value, RawInputModifiers.None); Pump(20);
            win.MouseDown(p.Value, MouseButton.Left, RawInputModifiers.LeftMouseButton); Pump(20);
            win.MouseUp(p.Value, MouseButton.Left, RawInputModifiers.None); Pump(20);
        }
        win.MouseMove(new Point(2, 2), RawInputModifiers.None);
        foreach (var combo in win.GetVisualDescendants().OfType<ComboBox>().Where(c => c.IsEffectivelyVisible).ToList()) { combo.IsDropDownOpen = true; Pump(40); combo.IsDropDownOpen = false; Pump(20); }
        foreach (var t in win.GetVisualDescendants().OfType<ToggleSwitch>().ToList()) { t.IsChecked = !(t.IsChecked ?? false); Pump(20); t.IsChecked = !(t.IsChecked ?? false); }
        foreach (var tab in win.GetVisualDescendants().OfType<TabControl>().ToList()) { var i = tab.SelectedIndex; tab.SelectedIndex = (i + 1) % tab.ItemCount; Pump(20); tab.SelectedIndex = i; }
        Pump(200);
    }

    static void Pump(int ms)
    {
        var until = DateTime.UtcNow.AddMilliseconds(ms);
        do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(4); Thread.Sleep(16); } while (DateTime.UtcNow < until);
        Dispatcher.UIThread.RunJobs();
    }

    static WriteableBitmap Capture(Window w)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        return w.CaptureRenderedFrame()!;
    }

    static void CropNamed(Window w, string fullPng, string name, string dest)
    {
        var c = w.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "sec" + char.ToUpper(name[0]) + name[1..]);
        if (c == null) { Console.WriteLine("no section " + name); return; }
        var p = c.TranslatePoint(new Point(0, 0), w);
        if (p == null) return;
        double s = w.RenderScaling; int pad = 20;
        int x = Math.Max(0, (int)((p.Value.X - pad) * s)), y = Math.Max(0, (int)((p.Value.Y - pad) * s));
        int ww = (int)((c.Bounds.Width + pad * 2) * s), hh = (int)((c.Bounds.Height + pad * 2) * s);
        using var bmp = SKBitmap.Decode(fullPng);
        ww = Math.Min(ww, bmp.Width - x); hh = Math.Min(hh, bmp.Height - y);
        if (ww <= 0 || hh <= 0) { Console.WriteLine($"section {name} outside capture"); return; }
        using var sub = new SKBitmap();
        bmp.ExtractSubset(sub, new SKRectI(x, y, x + ww, y + hh));
        using var img = SKImage.FromBitmap(sub);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(dest, data.ToArray());
    }
}
