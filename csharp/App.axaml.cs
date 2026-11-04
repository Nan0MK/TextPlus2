using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace TextPlus2;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppConfig.Load();
        if (AppConfig.Current.Theme == "light")
            RequestedThemeVariant = ThemeVariant.Light;
        else if (AppConfig.Current.Theme == "dark")
            RequestedThemeVariant = ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;
            if (desktop.Args is { Length: > 0 })
                window.OpenStartupFile(desktop.Args[0]);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
