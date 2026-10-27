using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using ChiselUI.Avalonia;
using ChiselUI.Avalonia.Controls;
using System.Runtime.InteropServices;
using System.Text;

namespace TextPlus2;

public partial class MainWindow : ChiselWindow
{
    private static readonly FilePickerFileType TextType = new("Text")
    {
        Patterns = ["*.txt", "*.md", "*.cs", "*.odin", "*.json", "*.log", "*.xml"],
        MimeTypes = ["text/plain"],
    };

    private const double BaseFontSize = 15;
    private const int ZoomMin = 50;
    private const int ZoomMax = 300;
    private const int ZoomStep = 10;
    private const double LogoSize = 22;

    private double _wheelAccum;
    private int _zoom = 100;
    private bool _suppress;
    private bool _allowClose;
    private int _dialogSession;
    private string _message = "";

    public MainWindow()
    {
        InitializeComponent();
        InitHighlighting();
        InitDocuments();
        AddHandler(PointerWheelChangedEvent, OnPreviewWheel, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnPreviewKey, RoutingStrategies.Tunnel);
        ActualThemeVariantChanged += (_, _) =>
        {
            UpdateThemeButton();
            ApplyEditorTheme();
        };
        SetZoom(100);
        UpdateThemeButton();
        ApplyEditorTheme();
        InitFileTree();
    }

    private async void New_Click(object? sender, RoutedEventArgs e) => await NewDocumentAsync();
    private async void Open_Click(object? sender, RoutedEventArgs e) => await OpenAsync();
    private async void OpenFolder_Click(object? sender, RoutedEventArgs e) => await OpenFolderAsync();
    private async void Save_Click(object? sender, RoutedEventArgs e) => await SaveAsync();
    private async void SaveAs_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();
    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    private void Undo_Click(object? sender, RoutedEventArgs e) => Editor.Undo();
    private void Redo_Click(object? sender, RoutedEventArgs e) => Editor.Redo();
    private void Cut_Click(object? sender, RoutedEventArgs e) => Editor.Cut();
    private void Copy_Click(object? sender, RoutedEventArgs e) => Editor.Copy();
    private void Paste_Click(object? sender, RoutedEventArgs e) => Editor.Paste();
    private void SelectAll_Click(object? sender, RoutedEventArgs e) => Editor.SelectAll();

    private void Find_Click(object? sender, RoutedEventArgs e) => ShowFind();
    private void FindNext_Click(object? sender, RoutedEventArgs e) => FindNext();
    private void CloseFind_Click(object? sender, RoutedEventArgs e) => CloseFind();

    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => SetZoom(_zoom + ZoomStep);
    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => SetZoom(_zoom - ZoomStep);
    private void ZoomReset_Click(object? sender, RoutedEventArgs e) => SetZoom(100);
    private void Theme_Click(object? sender, RoutedEventArgs e) => ChiselTheme.ToggleTheme();

    private bool _wordWrap;

    private void WordWrap_Click(object? sender, RoutedEventArgs e)
    {
        // A pointer click flips IsChecked before this runs. Assign the flag we own.
        _wordWrap = !_wordWrap;
        WordWrapMenuItem.IsChecked = _wordWrap;
        Editor.WordWrap = _wordWrap;
        FocusEditor();
    }

    private void FindBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FindNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseFind();
            e.Handled = true;
        }
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_suppress || _current == null)
            return;
        _dirty = true;
        UpdateChrome();
    }

    private async Task SaveAsync()
    {
        if (_file == null)
        {
            await SaveAsAsync();
            return;
        }

        try
        {
            await WriteAsync(_file);
            _dirty = false;
            SetStatus("Saved");
            UpdateChrome();
        }
        catch (Exception)
        {
            SetStatus("Could not save file");
        }
    }

    private async Task SaveAsAsync()
    {
        var file = await AfterModalAsync(() => StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            SuggestedFileName = string.IsNullOrEmpty(TitlePath) ? (_current?.DisplayName ?? "Untitled") + ".txt" : Path.GetFileName(TitlePath),
            FileTypeChoices = [TextType, FilePickerFileTypes.All],
            DefaultExtension = "txt",
        }));
        if (file == null)
        {
            SetStatus("Save canceled");
            FocusEditor();
            return;
        }

        var local = LocalPathOf(file);
        OpenDocument? replaced = null;
        if (!string.IsNullOrEmpty(local))
        {
            var other = FindDocument(local);
            if (other != null && !ReferenceEquals(other, _current))
            {
                if (other.Dirty && !await ConfirmDiscardAsync(other))
                {
                    SetStatus("Save canceled");
                    FocusEditor();
                    return;
                }

                replaced = other;
            }
        }

        try
        {
            await WriteAsync(file);
            _file = file;
            TitlePath = local ?? file.Name;
            _dirty = false;
            if (replaced != null)
                RemoveDocument(replaced);
            SetStatus("Saved");
            UpdateChrome();
            if (!string.IsNullOrEmpty(local))
                FollowDocument(local);
            ApplyGrammar(TitlePath);
            FocusEditor();
        }
        catch (Exception)
        {
            SetStatus("Could not save file");
            FocusEditor();
        }
    }

    private async Task WriteAsync(IStorageFile file)
    {
        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var text = Editor.Text ?? "";
        await writer.WriteAsync(text.Replace("\n", Environment.NewLine));
    }

    private void ShowFind()
    {
        FindBar.IsVisible = true;
        FindBox.Focus();
        FindBox.SelectAll();
    }

    private void CloseFind()
    {
        FindBar.IsVisible = false;
        Editor.TextArea.Focus();
    }

    private void FindNext()
    {
        var query = FindBox.Text ?? "";
        if (query.Length == 0)
        {
            ShowFind();
            SetStatus("Find: empty");
            return;
        }

        var text = Editor.Text ?? "";
        var selection = Editor.SelectedText ?? "";
        var start = selection == query ? Editor.SelectionStart + Editor.SelectionLength : Editor.SelectionStart;
        if (start < 0 || start > text.Length)
            start = 0;

        var index = text.IndexOf(query, start, StringComparison.Ordinal);
        var wrapped = false;
        if (index < 0 && start > 0)
        {
            index = text.IndexOf(query, 0, StringComparison.Ordinal);
            wrapped = index >= 0;
        }

        if (index < 0)
        {
            SetStatus("Find: not found");
            return;
        }

        Editor.Select(index, query.Length);
        Editor.TextArea.Caret.BringCaretToView();
        if (!FindBar.IsVisible)
            Editor.TextArea.Focus();
        SetStatus(wrapped ? "Find: wrapped" : "Find: match");
    }

    private void SetStatus(string message)
    {
        _message = message;
        UpdateChrome();
    }

    private void UpdateChrome()
    {
        ApplyGrammar(TitlePath);
        var name = _current?.DisplayName ?? "Untitled";
        var mark = _dirty ? "*" : "";
        Title = $"TextPlus2 - {name}{mark}";
        if (_current != null)
            RefreshTab(_current);

        var (line, col) = CaretLineCol();
        CaretText.Text = $"Ln {line}, Col {col}";
        NameText.Text = name;
        StateText.Text = _dirty ? "Modified" : "Saved";
        ZoomText.Text = $"{_zoom}%";
        AutomationProperties.SetName(ZoomCell, $"{_zoom}%");
        StatusText.Text = _message;
    }

    private void UpdateThemeButton()
    {
        var toLight = ActualThemeVariant == ThemeVariant.Dark;
        ThemeLabel.Text = toLight ? "Light" : "Dark";
        ThemeMenuItem.Header = toLight ? "Switch to _Light" : "Switch to _Dark";
        ToolTip.SetTip(ThemeButton, toLight ? "Switch to light" : "Switch to dark");
    }

    private void SetZoom(int percent)
    {
        _zoom = Math.Clamp(percent, ZoomMin, ZoomMax);
        Editor.FontSize = BaseFontSize * _zoom / 100.0;
        ApplyEditorFace();
        UpdateChrome();
    }

    private void OnPreviewKey(object? sender, KeyEventArgs e)
    {
        // The editor takes Tab for itself. Catch Ctrl+Tab on the way down so it changes documents.
        if (e.Handled || e.Key != Key.Tab || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            return;
        CycleDocuments(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
        e.Handled = true;
    }

    private void OnPreviewWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;
        e.Handled = true;
        _wheelAccum += e.Delta.Y;
        while (_wheelAccum >= 1)
        {
            SetZoom(_zoom + ZoomStep);
            _wheelAccum -= 1;
        }
        while (_wheelAccum <= -1)
        {
            SetZoom(_zoom - ZoomStep);
            _wheelAccum += 1;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            return;

        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        switch (e.Key)
        {
            case Key.Add:
            case Key.Subtract:
            case Key.NumPad0:
                break;
            case Key.OemPlus when shift:
                break;
            default:
                return;
        }

        if (e.Key is Key.Add or Key.OemPlus)
            SetZoom(_zoom + ZoomStep);
        else if (e.Key == Key.Subtract)
            SetZoom(_zoom - ZoomStep);
        else
            SetZoom(100);
        e.Handled = true;
    }

    private (int line, int col) CaretLineCol()
    {
        var caret = Editor.TextArea.Caret;
        return (Math.Max(1, caret.Line), Math.Max(1, caret.Column));
    }

    private static WindowIcon LoadWindowIcon()
    {
        using var stream = AssetLoader.Open(new Uri("avares://TextPlus2/Assets/TextPlus2Logo.ico"));
        return new WindowIcon(stream);
    }

    private static Image LogoImage()
    {
        using var stream = AssetLoader.Open(new Uri("avares://TextPlus2/Assets/TextPlus2Logo.png"));
        var image = new Image
        {
            Width = LogoSize,
            Height = LogoSize,
            Source = new Bitmap(stream),
        };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        return image;
    }

    private async Task<bool> ConfirmDiscardAsync(OpenDocument? only = null)
    {
        var pending = new List<OpenDocument>();
        if (only != null)
        {
            if (only.Dirty)
                pending.Add(only);
        }
        else
        {
            foreach (var doc in _docs)
            {
                if (doc.Dirty)
                    pending.Add(doc);
            }
        }

        if (pending.Count == 0)
            return true;

        var message = pending.Count == 1
            ? $"Discard unsaved changes to {pending[0].DisplayName}?"
            : $"Discard unsaved changes in {pending.Count} files?";

        var dialog = new ChiselWindow
        {
            Title = "Unsaved changes",
            Width = 440,
            Height = 210,
            CanResize = false,
            IsDialogFrame = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false,
            Icon = LoadWindowIcon(),
            IconContent = LogoImage(),
        };
        var discard = new Button { Content = "Discard", IsDefault = true, MinWidth = 88, Classes = { "primary" } };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 88 };
        discard.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { discard, cancel },
                },
            },
        };
        return await AfterModalAsync(() => dialog.ShowDialog<bool>(this));
    }

    private async Task<T> AfterModalAsync<T>(Func<Task<T>> show)
    {
        // A menu popup or another dialog is still closing on this turn. Starting a Win32
        // file dialog before that finishes leaves this window disabled: clicks beep and
        // the title-bar buttons do nothing, while the process keeps running.
        await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
        RestoreWindowInput();
        _dialogSession++;
        try
        {
            return await show();
        }
        finally
        {
            // ShowDialog completes inside its Closed handler. Enabling the owner inline
            // there aborts the dialog teardown and leaves that window modal.
            var session = _dialogSession;
            Dispatcher.UIThread.Post(() => RestoreIfCurrent(session), DispatcherPriority.Background);
            Dispatcher.UIThread.Post(() => RestoreIfCurrent(session), DispatcherPriority.ContextIdle);
            _ = DelayedRestoreAsync(session);
        }
    }

    private async Task DelayedRestoreAsync(int session)
    {
        await Task.Delay(250);
        if (session != _dialogSession)
            return;
        await Dispatcher.UIThread.InvokeAsync(() => RestoreIfCurrent(session));
    }

    private void RestoreIfCurrent(int session)
    {
        if (session == _dialogSession)
            RestoreWindowInput();
    }

    private void RestoreWindowInput()
    {
        IsEnabled = true;
        if (!OperatingSystem.IsWindows())
            return;
        var hwnd = TryGetPlatformHandle()?.Handle ?? 0;
        if (hwnd != 0)
            EnableWindow(hwnd, true);
    }

    private void FocusEditor()
    {
        if (!FindBar.IsVisible)
            Editor.TextArea.Focus();
    }

    [DllImport("user32.dll", EntryPoint = "EnableWindow")]
    private static extern bool EnableWindow(nint hWnd, bool bEnable);

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_allowClose || !_docs.Any(doc => doc.Dirty))
            return;
        e.Cancel = true;
        if (await ConfirmDiscardAsync())
        {
            _allowClose = true;
            Close();
        }
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n');
}
