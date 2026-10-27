using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace ChiselUI.Avalonia;

/// <summary>
/// The ChiselUI theme. Add one instance to Application.Styles. All tokens are generated from
/// <see cref="Accent"/> for both theme variants and live in Resources.ThemeDictionaries, so every
/// {DynamicResource Ch...} follows the active ThemeVariant and accent changes immediately.
/// </summary>
public class ChiselTheme : Styles
{
    /// <summary>The seven accent swatches of the web build.</summary>
    public static readonly IReadOnlyList<(string Name, Color Color)> Swatches = new[]
    {
        ("Aero blue", Color.Parse("#2f6bd8")),
        ("Teal", Color.Parse("#139a92")),
        ("Olive green", Color.Parse("#4a9a2c")),
        ("Violet", Color.Parse("#7a4fd8")),
        ("Ruby", Color.Parse("#d0453a")),
        ("Amber", Color.Parse("#e08a1a")),
        ("Graphite", Color.Parse("#5f6e82")),
    };

    public static readonly Color DefaultAccent = Color.Parse("#2f6bd8");
    /// <summary>The dark variant brightens the default accent like the web build (#2f6bd8 → #3d7bf0).</summary>
    public static readonly Color DefaultAccentDark = Color.Parse("#3d7bf0");

    /// <summary>The most recently created theme (normally the only one).</summary>
    public static ChiselTheme? Current { get; private set; }

    public event EventHandler? AccentChanged;

    readonly ResourceDictionary _light = new();
    readonly ResourceDictionary _dark = new();
    Color _accent = DefaultAccent;

    public ChiselTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
        // The shadow pad is inside the presenter, so the popup's top-left is not the card.
        // A MenuFlyout stored as a resource is never visited by styles, so pull it back here.
        Control.ContextFlyoutProperty.Changed.Subscribe(FlyoutOffset.Instance);
        AddMetrics(Resources);
        Resources.ThemeDictionaries[ThemeVariant.Light] = _light;
        Resources.ThemeDictionaries[ThemeVariant.Dark] = _dark;
        Rebuild();
        Current = this;
    }

    /// <summary>Accent colour (any colour works; the 7 <see cref="Swatches"/> are the curated set).</summary>
    public Color Accent
    {
        get => _accent;
        set { if (_accent != value) { _accent = value; Rebuild(); AccentChanged?.Invoke(this, EventArgs.Empty); } }
    }

    void Rebuild()
    {
        new ChiselTokens(false, _accent).ApplyTo(_light);
        var darkAccent = _accent == DefaultAccent ? DefaultAccentDark : _accent;
        new ChiselTokens(true, darkAccent).ApplyTo(_dark);
    }

    static void AddMetrics(IResourceDictionary r)
    {
        r["ChRadiusXs"] = new CornerRadius(3);
        r["ChRadiusSm"] = new CornerRadius(4);
        r["ChRadiusMd"] = new CornerRadius(6);
        r["ChRadiusLg"] = new CornerRadius(9);
        r["ChRadiusXl"] = new CornerRadius(14);
        r["ChRadiusFull"] = new CornerRadius(999);
        r["ChFontSizeXs"] = 11.0;
        r["ChFontSizeSm"] = 12.0;
        r["ChFontSizeBase"] = 13.0;
        r["ChFontSizeMd"] = 14.0;
        r["ChFontSizeLg"] = 16.0;
        r["ChFontSizeXl"] = 20.0;
        r["ChFontSize2xl"] = 26.0;
        r["ChControlHeight"] = 26.0;
        r["ChControlHeightSm"] = 22.0;
        r["ChRowHeight"] = 22.0;
        r["ChGapDock"] = 6.0;
        r["ChGlassFrameWidth"] = 10.0;
        r["ChGlassFrameDepth"] = 3.0;
        r["ChGlassFramePadding"] = new Thickness(10, 0, 10, 10);
        r["ChFontSans"] = new FontFamily("fonts:Inter#Inter, Segoe UI, sans-serif");
        r["ChFontMono"] = new FontFamily("avares://ChiselUI.Avalonia/Assets/Fonts#JetBrains Mono, Cascadia Mono, Consolas, monospace");
    }

    // ---- static helpers -------------------------------------------------------------------

    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>Sets Application.RequestedThemeVariant (Light, Dark or Default = follow the OS).</summary>
    public static void SetTheme(ThemeVariant variant)
    {
        if (Application.Current is { } app) app.RequestedThemeVariant = variant;
    }

    /// <summary>Flips light/dark live and returns the new variant.</summary>
    public static ThemeVariant ToggleTheme()
    {
        var next = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
        SetTheme(next);
        return next;
    }

    /// <summary>Changes the accent of the active ChiselTheme live.</summary>
    public static void SetAccent(Color accent)
    {
        if (Current is { } t) t.Accent = accent;
    }
}

sealed class FlyoutOffset : IObserver<AvaloniaPropertyChangedEventArgs<FlyoutBase?>>
{
    public static readonly FlyoutOffset Instance = new();

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(AvaloniaPropertyChangedEventArgs<FlyoutBase?> value)
    {
        if (value.NewValue.Value is MenuFlyout flyout)
        {
            flyout.HorizontalOffset = -34;
            flyout.VerticalOffset = -16;
        }
    }
}

public static class ChiselAppBuilderExtensions
{
    /// <summary>Registers the bundled Inter font that ChiselUI uses (same as .WithInterFont()).</summary>
    public static AppBuilder UseChiselUI(this AppBuilder builder) => builder.WithInterFont();
}
