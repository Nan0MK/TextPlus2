using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace TextPlus2;

public partial class MainWindow
{
    private RegistryOptions? _registryOptions;
    private TextMate.Installation? _highlight;
    private string? _grammarKey;

    private void InitHighlighting()
    {
        // A grammar failure should leave the text readable instead of taking the window down.
        TextMate.RegisterExceptionHandler(static _ => { });
        _registryOptions = new RegistryOptions(EditorThemeName());
        _highlight = Editor.InstallTextMate(_registryOptions);
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
        if (_highlight == null)
            return;

        _highlight.SetTheme(new RegistryOptions(EditorThemeName()).GetDefaultTheme());
        if (ThemeBrush("ChTextBrush") is IBrush brush)
            Editor.TextArea.CaretBrush = brush;
    }

    private void ApplyEditorFace()
    {
        var font = Editor.FontFamily;
        var size = Editor.FontSize;
        SetFace(Editor.TextArea.TextView, font, size);
        foreach (var margin in Editor.TextArea.LeftMargins)
            SetFace(margin, font, size);
    }

    private static void SetFace(Control target, FontFamily font, double size)
    {
        TextElement.SetFontFamily(target, font);
        TextElement.SetFontWeight(target, FontWeight.SemiBold);
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
