using Avalonia.Controls;
using ChiselUI.Avalonia.Controls;

namespace ChiselUI.Demo;

/// <summary>The file explorer as real window chrome: ChiselWindow = the glass frame is the OS window's non-client area.</summary>
public class ExplorerWindow : ChiselWindow
{
    public ExplorerWindow()
    {
        Title = "Documents";
        Width = 1000; Height = 640; MinWidth = 640; MinHeight = 420;
        IconContent = new DemoIcon { Kind = "library", Size = 14 };
        Content = new ExplorerView();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
