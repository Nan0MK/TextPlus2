using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace TextPlus2;

public partial class MainWindow
{
    private RegistryOptions? _registryOptions;
    private TextMate.Installation? _highlight;
    private ThemeName? _appliedTheme;
    private string? _grammarKey;

    private void InitHighlighting()
    {
        // A grammar failure should leave the text readable instead of taking the window down.
        TextMate.RegisterExceptionHandler(static _ => { });
        // One registry for the session. InstallTextMate applies its default theme.
        // A theme change loads another theme file from this same registry.
        var theme = EditorThemeName();
        _registryOptions = new RegistryOptions(theme);
        _highlight = Editor.InstallTextMate(_registryOptions);
        _appliedTheme = theme;
        Editor.TextChanged += Editor_TextChanged;
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateChrome();
        Editor.Loaded += (_, _) =>
        {
            // AvaloniaEdit opens its own find bar on Ctrl+F. This window already has one.
            Editor.SearchPanel?.Uninstall();
            ApplyEditorFace();
            ApplyEditorTheme();
        };
        Editor.TextArea.ContextMenu = new ContextMenu
        {
            Items =
            {
                EditorMenu("Cu_t", Cut_Click),
                EditorMenu("_Copy", Copy_Click),
                EditorMenu("_Paste", Paste_Click),
                EditorMenu("Select _All", SelectAll_Click),
            },
        };
    }

    private void ApplyGrammar(string? path)
    {
        if (_highlight == null || _registryOptions == null)
            return;

        var key = ExtensionKey(path);
        if (key == _grammarKey)
            return;
        _grammarKey = key;

        var scope = ScopeFor(key);
        if (scope == null)
        {
            ClearGrammar();
            return;
        }

        try
        {
            _highlight.SetGrammar(scope);
        }
        catch (Exception)
        {
            ClearGrammar();
        }
    }

    private void ApplyEditorTheme()
    {
        if (_highlight == null || _registryOptions == null)
            return;

        var theme = EditorThemeName();
        if (_appliedTheme != theme)
        {
            var raw = _registryOptions.LoadTheme(theme);
            if (raw != null)
            {
                _highlight.SetTheme(raw);
                _appliedTheme = theme;
            }
        }

        if (ThemeBrush("ChTextBrush") is IBrush brush)
            Editor.TextArea.CaretBrush = brush;
    }

    private void Font_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || item.Tag is not string key)
            return;

        // A pointer click flips IsChecked before this runs. The chosen face stays checked.
        SelectFont(key);
        FocusEditor();
    }

    private void SelectFont(string key)
    {
        if (key is not ("jetbrains" or "cascadia" or "plex"))
            key = "jetbrains";
        _editorFont = key;
        ApplyEditorFont(key);
        FontJetBrainsMenuItem.IsChecked = key == "jetbrains";
        FontCascadiaMenuItem.IsChecked = key == "cascadia";
        FontPlexMenuItem.IsChecked = key == "plex";
        RememberOptions();
    }

    private void ApplyEditorFont(string key)
    {
        // Each resource is one static file. The weight is that file's OS/2 weight.
        var (resource, weight) = key switch
        {
            "cascadia" => ("EditorFontCascadia", FontWeight.Normal),
            "plex" => ("EditorFontPlex", FontWeight.Medium),
            _ => ("EditorFont", FontWeight.Medium),
        };
        if (this.TryFindResource(resource, out var value) && value is FontFamily family)
            Editor.FontFamily = family;
        Editor.FontWeight = weight;
        ApplyEditorFace();
    }

    private void ApplyEditorFace()
    {
        var font = Editor.FontFamily;
        var weight = Editor.FontWeight;
        var size = Editor.FontSize;
        SetFace(Editor.TextArea.TextView, font, weight, size);
        // The margin measures itself to the digits and draws them flush right. Padding is ignored.
        var after = DigitWidth(font, weight, size) * 2;
        var before = size * 0.3;
        foreach (var margin in Editor.TextArea.LeftMargins)
        {
            SetFace(margin, font, weight, size);
            if (margin is LineNumberMargin)
                margin.Margin = new Thickness(before, 0, after, 0);
        }
    }

    private static double DigitWidth(FontFamily font, FontWeight weight, double size)
    {
        var text = new FormattedText(
            "0",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(font, FontStyle.Normal, weight),
            size,
            null);
        return text.Width;
    }

    private static void SetFace(Control target, FontFamily font, FontWeight weight, double size)
    {
        TextElement.SetFontFamily(target, font);
        TextElement.SetFontWeight(target, weight);
        TextElement.SetFontSize(target, size);
    }

    private ThemeName EditorThemeName() =>
        ActualThemeVariant == ThemeVariant.Light ? ThemeName.LightPlus : ThemeName.DarkPlus;

    private string? ScopeFor(string extension)
    {
        if (extension.Length == 0 || _registryOptions == null)
            return null;

        var language = _registryOptions.GetLanguageByExtension(extension)
            ?? _registryOptions.GetLanguageByExtension(extension.TrimStart('.'));
        if (language == null)
            return null;

        var scope = _registryOptions.GetScopeByLanguageId(language.Id);
        return string.IsNullOrEmpty(scope) ? null : scope;
    }

    private void ClearGrammar()
    {
        var transformer = Editor.TextArea.TextView.LineTransformers
            .OfType<TextMateColoringTransformer>()
            .FirstOrDefault();
        if (transformer == null)
            return;

        try
        {
            transformer.SetGrammar(null!);
            Editor.TextArea.TextView.Redraw();
        }
        catch (Exception)
        {
            // Plain text keeps the editor foreground.
        }
    }

    private IBrush? ThemeBrush(string key)
    {
        if (this.TryFindResource(key, ActualThemeVariant, out var themed) && themed is IBrush themedBrush)
            return themedBrush;
        if (this.TryFindResource(key, out var value) && value is IBrush brush)
            return brush;
        return null;
    }

    private static string ExtensionKey(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        try
        {
            return Path.GetExtension(path).ToLowerInvariant();
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static MenuItem EditorMenu(string header, EventHandler<RoutedEventArgs> onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += onClick;
        return item;
    }
}
