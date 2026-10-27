using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using ChiselUI.Avalonia;
using ChiselUI.Avalonia.Controls;

namespace ChiselUI.Demo;

public partial class MainWindow : ChiselWindow
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateThemeButton();
        Clock.Text = DateTime.Now.ToString("h:mm tt");
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        t.Tick += (_, _) => Clock.Text = DateTime.Now.ToString("h:mm tt");
        t.Start();
        ActualThemeVariantChanged += (_, _) => UpdateThemeButton();
    }

    void UpdateThemeButton() => ThemeLabel.Text = ActualThemeVariant == ThemeVariant.Dark ? "Light" : "Dark";

    void OnToggleTheme(object? s, RoutedEventArgs e) => ChiselTheme.ToggleTheme();

    public void ShowPage(bool explorer)
    {
        Showcase.IsVisible = !explorer;
        Explorer.IsVisible = explorer;
        TabShowcase.IsChecked = !explorer;
        TabExplorer.IsChecked = explorer;
    }

    void OnShowcase(object? s, RoutedEventArgs e) => ShowPage(false);
    void OnExplorer(object? s, RoutedEventArgs e) => ShowPage(true);

    void OnRealExplorer(object? s, RoutedEventArgs e) => new ExplorerWindow().Show(this);
}
