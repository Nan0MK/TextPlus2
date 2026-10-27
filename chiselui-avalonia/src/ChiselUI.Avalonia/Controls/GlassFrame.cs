using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ChiselUI.Avalonia.Controls;

/// <summary>
/// The ChiselUI window body: a thick frosted-glass frame (10px, no outlines) with a darker accent
/// depth band, a title bar on the slab face, a Vista/7 caption capsule and an opaque client area
/// lifted on top of the frame. Used standalone for in-app windows/dialogs, and by <see cref="ChiselWindow"/>
/// as real window chrome (then the title bar drags, double-click maximizes and the captions drive the window).
/// </summary>
[TemplatePart("PART_TitleBar", typeof(Control))]
[TemplatePart("PART_Minimize", typeof(Button))]
[TemplatePart("PART_Maximize", typeof(Button))]
[TemplatePart("PART_Close", typeof(Button))]
[PseudoClasses(":dialog", ":chrome", ":glass", ":inactive", ":maximized")]
public class GlassFrame : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<GlassFrame, string?>(nameof(Title));
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<GlassFrame, object?>(nameof(Icon));
    public static readonly StyledProperty<object?> TitleBarContentProperty = AvaloniaProperty.Register<GlassFrame, object?>(nameof(TitleBarContent));
    public static readonly StyledProperty<bool> IsDialogProperty = AvaloniaProperty.Register<GlassFrame, bool>(nameof(IsDialog));
    public static readonly StyledProperty<bool> IsInactiveProperty = AvaloniaProperty.Register<GlassFrame, bool>(nameof(IsInactive));
    public static readonly StyledProperty<bool> ShowMinMaxProperty = AvaloniaProperty.Register<GlassFrame, bool>(nameof(ShowMinMax), true);
    public static readonly StyledProperty<bool> ShowCaptionButtonsProperty = AvaloniaProperty.Register<GlassFrame, bool>(nameof(ShowCaptionButtons), true);

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    /// <summary>Extra content placed in the title bar between the title and the caption buttons.</summary>
    public object? TitleBarContent { get => GetValue(TitleBarContentProperty); set => SetValue(TitleBarContentProperty, value); }
    public bool IsDialog { get => GetValue(IsDialogProperty); set => SetValue(IsDialogProperty, value); }
    public bool IsInactive { get => GetValue(IsInactiveProperty); set => SetValue(IsInactiveProperty, value); }
    public bool ShowMinMax { get => GetValue(ShowMinMaxProperty); set => SetValue(ShowMinMaxProperty, value); }
    public bool ShowCaptionButtons { get => GetValue(ShowCaptionButtonsProperty); set => SetValue(ShowCaptionButtonsProperty, value); }

    /// <summary>Raised when the close caption is clicked on an in-app (non-chrome) frame.</summary>
    public event EventHandler<RoutedEventArgs>? CloseRequested;

    internal Window? HostWindow { get; set; }

    Control? _titleBar;
    Button? _min, _max, _close;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_titleBar != null) { _titleBar.PointerPressed -= OnTitlePressed; _titleBar.DoubleTapped -= OnTitleDoubleTapped; }
        if (_min != null) _min.Click -= OnMin;
        if (_max != null) _max.Click -= OnMax;
        if (_close != null) _close.Click -= OnClose;
        _titleBar = e.NameScope.Find<Control>("PART_TitleBar");
        _min = e.NameScope.Find<Button>("PART_Minimize");
        _max = e.NameScope.Find<Button>("PART_Maximize");
        _close = e.NameScope.Find<Button>("PART_Close");
        if (_titleBar != null) { _titleBar.PointerPressed += OnTitlePressed; _titleBar.DoubleTapped += OnTitleDoubleTapped; }
        if (_min != null) _min.Click += OnMin;
        if (_max != null) _max.Click += OnMax;
        if (_close != null) _close.Click += OnClose;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDialogProperty) PseudoClasses.Set(":dialog", IsDialog);
        else if (change.Property == IsInactiveProperty) PseudoClasses.Set(":inactive", IsInactive);
    }

    internal void SetChromeState(bool chrome, bool glass, bool maximized)
    {
        PseudoClasses.Set(":chrome", chrome);
        PseudoClasses.Set(":glass", glass);
        PseudoClasses.Set(":maximized", maximized);
    }

    void OnTitlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (HostWindow is { } w && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount == 1)
            w.BeginMoveDrag(e);
    }

    void OnTitleDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (HostWindow is { CanResize: true } w)
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    void OnMin(object? s, RoutedEventArgs e) { if (HostWindow is { } w) w.WindowState = WindowState.Minimized; }
    void OnMax(object? s, RoutedEventArgs e) { if (HostWindow is { } w) w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
    void OnClose(object? s, RoutedEventArgs e)
    {
        if (HostWindow is { } w) w.Close();
        else CloseRequested?.Invoke(this, e);
    }
}
