using System;
using Avalonia;
using ChiselUI.Avalonia;

namespace ChiselUI.Demo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().UseChiselUI().LogToTrace();
}
