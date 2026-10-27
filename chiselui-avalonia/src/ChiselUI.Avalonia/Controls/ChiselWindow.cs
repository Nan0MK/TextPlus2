using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace ChiselUI.Avalonia.Controls;

/// <summary>
/// A Window whose whole non-client area is the ChiselUI glass frame. Uses ExtendClientAreaToDecorationsHint
/// with NoChrome; title-bar drag, double-click maximize, edge resize and the min/max/close capsule are
/// implemented here. Requests OS blur (AcrylicBlur, then Mica, then Blur) and falls back to an opaque
/// tinted frame when the platform cannot blur (Windows 10 without acrylic, Linux, headless).
/// </summary>
public class ChiselWindow : Window
{
    public static readonly StyledProperty<object?> IconContentProperty = AvaloniaProperty.Register<ChiselWindow, object?>(nameof(IconContent));
    public static readonly StyledProperty<object?> TitleBarContentProperty = AvaloniaProperty.Register<ChiselWindow, object?>(nameof(TitleBarContent));
    public static readonly StyledProperty<bool> IsDialogFrameProperty = AvaloniaProperty.Register<ChiselWindow, bool>(nameof(IsDialogFrame));
    public static readonly StyledProperty<bool> UseOsGlassProperty = AvaloniaProperty.Register<ChiselWindow, bool>(nameof(UseOsGlass), true);

    /// <summary>Small icon shown at the left of the title (any content, e.g. a PathIcon).</summary>
    public object? IconContent { get => GetValue(IconContentProperty); set => SetValue(IconContentProperty, value); }
    public object? TitleBarContent { get => GetValue(TitleBarContentProperty); set => SetValue(TitleBarContentProperty, value); }
    /// <summary>Dialog look for the frame (deeper shadow, dialog client colour). Independent of ShowDialog.</summary>
    public bool IsDialogFrame { get => GetValue(IsDialogFrameProperty); set => SetValue(IsDialogFrameProperty, value); }
    /// <summary>Ask the OS for acrylic/mica behind the frame (default true). False forces the tinted fallback.</summary>
    public bool UseOsGlass { get => GetValue(UseOsGlassProperty); set => SetValue(UseOsGlassProperty, value); }

    protected override Type StyleKeyOverride => typeof(ChiselWindow);

    GlassFrame? _frame;

    public ChiselWindow()
    {
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;
        ApplyGlassHint();
    }

    void ApplyGlassHint()
    {
        TransparencyLevelHint = UseOsGlass
            ? new[] { WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Mica, WindowTransparencyLevel.Blur, WindowTransparencyLevel.None }
            : new[] { WindowTransparencyLevel.None };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _frame = e.NameScope.Find<GlassFrame>("PART_Frame");
        if (_frame != null) _frame.HostWindow = this;
        foreach (var (name, edge) in new[] {
            ("PART_ResizeN", WindowEdge.North), ("PART_ResizeS", WindowEdge.South), ("PART_ResizeW", WindowEdge.West), ("PART_ResizeE", WindowEdge.East),
            ("PART_ResizeNW", WindowEdge.NorthWest), ("PART_ResizeNE", WindowEdge.NorthEast), ("PART_ResizeSW", WindowEdge.SouthWest), ("PART_ResizeSE", WindowEdge.SouthEast) })
        {
            if (e.NameScope.Find<Control>(name) is { } grip)
            {
                var ed = edge;
                grip.PointerPressed += (_, pe) =>
                {
                    if (CanResize && WindowState == WindowState.Normal && pe.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    { BeginResizeDrag(ed, pe); pe.Handled = true; }
                };
            }
        }
        UpdateChrome();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualTransparencyLevelProperty || change.Property == WindowStateProperty || change.Property == IsActiveProperty)
            UpdateChrome();
        else if (change.Property == UseOsGlassProperty)
            ApplyGlassHint();
    }

    void UpdateChrome()
    {
        var glass = ActualTransparencyLevel != WindowTransparencyLevel.None && ActualTransparencyLevel != WindowTransparencyLevel.Transparent;
        PseudoClasses.Set(":glass", glass);
        if (_frame == null) return;
        _frame.SetChromeState(true, glass, WindowState == WindowState.Maximized);
        _frame.IsInactive = !IsActive;
    }
}
