using Avalonia;
using ChiselUI.Avalonia;
using System;

namespace TextPlus2;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseChiselUI()
            .LogToTrace();
}
