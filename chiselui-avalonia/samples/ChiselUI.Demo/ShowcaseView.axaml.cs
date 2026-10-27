using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace ChiselUI.Demo;

public record BuildMessage(string Icon, string File, string Line, string Message);

public partial class ShowcaseView : UserControl
{
    static readonly string[] Code =
    {
        "#include <iostream>",
        "#include \"math.h\"",
        "",
        "int addem(int, int);",
        "",
        "int main()",
        "{",
        "    int x, y, z;",
        "    double d;",
        "    // read two numbers from the user",
        "    std::cout << \"Enter two numbers: \";",
        "    std::cin >> x >> y;",
        "",
        "    if (x < 0 || y < 0) {",
        "        std::cerr << \"positive only\\n\";",
        "        return 1;",
        "    }",
        "    z = addem(x, y);",
        "    std::cout << x << \" + \" << y << \" = \" << z << std::endl;",
        "    return 0;",
        "}",
    };

    static readonly Regex Tok = new(@"(?<com>//.*$)|(?<pp>^\s*#\w+)|(?<str>""(?:\\.|[^""])*""|<[a-z]+>)|(?<num>\b\d+\b)|(?<type>\b(?:int|double|void|char|bool|auto)\b)|(?<kw>\b(?:if|else|return|for|while|std|const)\b)|(?<id>[A-Za-z_]\w*|.)", RegexOptions.Compiled);

    public ShowcaseView()
    {
        InitializeComponent();
        BuildEditor();
        BuildGrid.ItemsSource = new List<BuildMessage>
        {
            new("info", "main.cpp", "", "In function 'int main()':"),
            new("warn", "main.cpp", "9", "warning: unused variable 'd' [-Wunused-variable]"),
            new("warn", "math.cpp", "14", "warning: comparison of integer expressions of different signedness"),
            new("warn", "math.h", "3", "warning: 'inline' is not at beginning of declaration"),
            new("info", "—", "", "=== Build finished: 0 errors, 3 warnings (0 minute(s), 1 second(s)) ==="),
            new("info", "—", "", "Executing: bin/Debug/hello (in /home/jake/projects/hello)"),
        };
        BuildGrid.SelectedIndex = 1;
        FilesGrid.ItemsSource = new List<FileItem>
        {
            new("main.cpp", "", "cpp", "C++ source", "1.2 KB", "10:39 PM"),
            new("math.cpp", "", "cpp", "C++ source", "842 B", "10:12 PM"),
            new("math.h", "", "h", "C header", "214 B", "Yesterday"),
            new("app.rc", "", "file", "Resource script", "96 B", "Oct 21"),
            new("obj", "", "folder", "File folder", "—", "10:40 PM"),
            new("bin", "", "folder", "File folder", "—", "10:40 PM"),
        };
        FilesGrid.SelectedIndex = 1;
        foreach (var (key, label) in new[] { ("ChAccentBrush", "accent"), ("ChAccentHiBrush", "accent-hi"), ("ChAccentLoBrush", "accent-lo"),
                                             ("ChSuccessBrush", "success"), ("ChWarningBrush", "warning"), ("ChDangerBrush", "danger") })
        {
            var chip = new Border { Height = 30, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 2) };
            chip.Classes.Add("elev1");
            chip.Bind(Border.BackgroundProperty, chip.GetResourceObservable(key));
            var lbl = new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 8) };
            lbl.Classes.Add("muted"); lbl.Classes.Add("xs");
            TokenGrid.Children.Add(new StackPanel { Margin = new Thickness(4, 2), Children = { chip, lbl } });
        }
    }

    void BuildEditor()
    {
        const int current = 19, breakpoint = 9;
        for (int i = 0; i < Code.Length; i++) EditorGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int i = 0; i < Code.Length; i++)
        {
            int n = i + 1;
            if (n == current)
            {
                var band = new Border();
                band.Bind(Border.BackgroundProperty, band.GetResourceObservable("ChCurrentLineBrush"));
                Grid.SetRow(band, i); Grid.SetColumnSpan(band, 3);
                EditorGrid.Children.Add(band);
            }
            var gutter = new TextBlock { Text = n.ToString(), TextAlignment = TextAlignment.Right, Padding = new Thickness(0, 0, 10, 0), FontSize = 11 };
            gutter.Bind(TextBlock.ForegroundProperty, gutter.GetResourceObservable(n == current ? "ChTextBrush" : "ChGutterTextBrush"));
            if (n == current) gutter.FontWeight = FontWeight.Bold;
            var gutterBg = new Border { Child = gutter, VerticalAlignment = VerticalAlignment.Stretch };
            gutterBg.Bind(Border.BackgroundProperty, gutterBg.GetResourceObservable("ChGutterBrush"));
            Grid.SetRow(gutterBg, i);
            EditorGrid.Children.Add(gutterBg);
            Control? mark = n == breakpoint
                ? new Ellipse { Width = 9, Height = 9, Fill = new RadialGradientBrush { GradientStops = { new(Color.Parse("#ff8a7a"), 0), new(Color.Parse("#c8231a"), 1) } } }
                : n == current ? new Path { Data = Geometry.Parse("M0,0 L4,0 L8,4.5 L4,9 L0,9 Z"), Fill = new SolidColorBrush(Color.Parse("#f0b020")) }
                : n is 7 or 14 ? new TextBlock { Text = "−", FontSize = 10, Opacity = .55 } : null;
            if (mark != null)
            {
                mark.HorizontalAlignment = HorizontalAlignment.Center; mark.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetRow(mark, i); Grid.SetColumn(mark, 1);
                EditorGrid.Children.Add(mark);
            }
            var line = new TextBlock { Padding = new Thickness(6, 0, 16, 0), LineHeight = 18 };
            foreach (Match m in Tok.Matches(Code[i]))
            {
                var r = new Run(m.Value);
                string? key = m.Groups["com"].Success ? "ChSynComBrush" : m.Groups["pp"].Success ? "ChSynPpBrush" : m.Groups["str"].Success ? "ChSynStrBrush"
                    : m.Groups["num"].Success ? "ChSynNumBrush" : m.Groups["type"].Success ? "ChSynTypeBrush" : m.Groups["kw"].Success ? "ChSynKwBrush" : null;
                if (key != null) r.Bind(TextElement.ForegroundProperty, line.GetResourceObservable(key));
                if (m.Groups["com"].Success) r.FontStyle = FontStyle.Italic;
                line.Inlines!.Add(r);
            }
            if (line.Inlines!.Count == 0) line.Text = " ";
            Grid.SetRow(line, i); Grid.SetColumn(line, 2);
            EditorGrid.Children.Add(line);
        }
    }
}
