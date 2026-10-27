using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ChiselUI.Demo;

/// <summary>Inline alert: tinted panel (status colour at low opacity over the raised surface) + gel status icon.</summary>
public class Alert : Border
{
    public string Kind { get; set; } = "info";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Classes.Add("raised");
        CornerRadius = new CornerRadius(6);
        var key = Kind switch { "ok" => "ChSuccessBrush", "warn" => "ChWarningBrush", "error" => "ChDangerBrush", _ => "ChAccentBrush" };
        var tint = new Border { CornerRadius = new CornerRadius(6), Opacity = .13 };
        tint.Bind(Border.BackgroundProperty, tint.GetResourceObservable(key));
        var bar = new Border { Width = 3, CornerRadius = new CornerRadius(6, 0, 0, 6), HorizontalAlignment = HorizontalAlignment.Left };
        bar.Bind(Border.BackgroundProperty, bar.GetResourceObservable(key));
        var title = new TextBlock { Text = Title, FontWeight = FontWeight.SemiBold, FontSize = 12.5 };
        var body = new TextBlock { Text = Text, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        body.Classes.Add("secondary");
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(12, 7, 10, 8) };
        grid.Children.Add(new DemoIcon { Kind = Kind, Size = 18, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 10, 0) });
        var sp = new StackPanel { Spacing = 1 };
        sp.Children.Add(title); sp.Children.Add(body);
        Grid.SetColumn(sp, 1);
        grid.Children.Add(sp);
        Child = new Panel { Children = { tint, bar, grid } };
    }
}
