using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Document;

namespace TextPlus2;

public partial class MainWindow
{
    private readonly List<OpenDocument> _docs = new();
    private OpenDocument? _current;
    private bool _suppressTabs;
    private int _loadSerial;

    private IStorageFile? _file
    {
        get => _current?.File;
        set
        {
            if (_current != null)
                _current.File = value;
        }
    }

    private bool _dirty
    {
        get => _current?.Dirty == true;
        set
        {
            if (_current != null)
                _current.Dirty = value;
        }
    }

    private string TitlePath
    {
        get => _current?.TitlePath ?? "";
        set
        {
            if (_current != null)
                _current.TitlePath = value;
        }
    }

    private void InitDocuments()
    {
        // One editor, one TextDocument per tab, so undo and the caret stay with that file.
        Editor.Document ??= new TextDocument();
        var doc = CreateDocument(Editor.Document, NextUntitledName());
        _docs.Add(doc);
        _suppressTabs = true;
        Documents.Items.Add(doc.Tab);
        Documents.SelectedItem = doc.Tab;
        _suppressTabs = false;
        _current = doc;
        AttachEditor(doc.Tab);
        RefreshTab(doc);
    }

    private void Documents_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTabs || sender is not TabControl tabs || Editor == null)
            return;
        if (tabs.SelectedItem is not TabItem tab || tab.Tag is not OpenDocument doc || ReferenceEquals(doc, _current))
            return;
        Activate(doc, restoreView: true);
        FocusEditor();
    }

    private async void Close_Click(object? sender, RoutedEventArgs e)
    {
        if (_current != null)
            await CloseDocumentAsync(_current);
    }

    private void CycleDocuments(int delta)
    {
        if (_docs.Count < 2 || _current == null)
            return;
        var index = _docs.IndexOf(_current);
        if (index < 0)
            index = 0;
        var next = index + delta;
        if (next >= _docs.Count)
            next = 0;
        else if (next < 0)
            next = _docs.Count - 1;
        Activate(_docs[next], restoreView: true);
        FocusEditor();
    }

    private Task NewDocumentAsync()
    {
        var doc = AddBlankDocument();
        Activate(doc, restoreView: false);
        SetStatus("New file");
        FocusEditor();
        return Task.CompletedTask;
    }

    private async Task CloseDocumentAsync(OpenDocument doc)
    {
        if (!_docs.Contains(doc))
            return;
        if (_docs.Count == 1 && IsPristine(doc))
            return;
        if (doc.Dirty && !await ConfirmDiscardAsync(doc))
        {
            FocusEditor();
            return;
        }
        if (!_docs.Contains(doc))
            return;

        var index = _docs.IndexOf(doc);
        var wasCurrent = ReferenceEquals(doc, _current);
        _docs.RemoveAt(index);
        _suppressTabs = true;
        Documents.Items.Remove(doc.Tab);
        _suppressTabs = false;

        if (_docs.Count == 0)
        {
            Activate(AddBlankDocument(), restoreView: false);
        }
        else if (wasCurrent)
        {
            Activate(_docs[Math.Min(index, _docs.Count - 1)], restoreView: true);
        }

        SetStatus("Closed");
        FocusEditor();
    }

    private void RemoveDocument(OpenDocument doc)
    {
        if (ReferenceEquals(doc, _current) || !_docs.Contains(doc))
            return;
        _docs.Remove(doc);
        _suppressTabs = true;
        Documents.Items.Remove(doc.Tab);
        _suppressTabs = false;
    }

    private async Task<LoadedFile?> ReadForOpenAsync(string? localPath, IStorageFile? known)
    {
        try
        {
            string text;
            if (known != null)
            {
                await using var stream = await known.OpenReadAsync();
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                text = await reader.ReadToEndAsync();
            }
            else
            {
                if (string.IsNullOrEmpty(localPath))
                    return null;
                text = await File.ReadAllTextAsync(localPath);
            }

            var file = known;
            if (file == null && !string.IsNullOrEmpty(localPath) && StorageProvider.CanOpen)
            {
                try
                {
                    file = await StorageProvider.TryGetFileFromPathAsync(localPath);
                }
                catch (Exception)
                {
                    file = null;
                }
            }

            var title = !string.IsNullOrEmpty(localPath) ? localPath : known?.Name ?? "";
            return new LoadedFile(Normalize(text), file, title);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private LoadResult CommitLoaded(LoadedFile loaded, bool show)
    {
        if (FindDocument(loaded.TitlePath) is { } existing)
        {
            if (show)
                Activate(existing, restoreView: true);
            return LoadResult.Same;
        }

        var doc = show && _docs.Count == 1 && _current != null && IsPristine(_current)
            ? _current
            : AddBlankDocument();
        doc.Document = new TextDocument(loaded.Text);
        doc.File = loaded.File;
        doc.TitlePath = loaded.TitlePath;
        doc.Dirty = false;
        doc.Caret = 0;
        doc.SelectionStart = 0;
        doc.SelectionLength = 0;
        var generation = ++_loadSerial;
        doc.LoadGeneration = generation;
        var expected = loaded.Text;
        Dispatcher.UIThread.Post(() =>
        {
            if (doc.LoadGeneration != generation)
                return;
            if (Normalize(doc.Document.Text) != expected)
                return;
            doc.Dirty = false;
            if (ReferenceEquals(doc, _current))
                UpdateChrome();
            else
                RefreshTab(doc);
        }, DispatcherPriority.ContextIdle);

        if (show)
        {
            Activate(doc, restoreView: true);
            SetStatus("Opened");
        }
        else
        {
            RefreshTab(doc);
        }

        return LoadResult.Opened;
    }

    private void Activate(OpenDocument doc, bool restoreView)
    {
        if (_current != null && !ReferenceEquals(_current, doc))
            RememberView(_current);

        var documentChanged = !ReferenceEquals(Editor.Document, doc.Document);
        _current = doc;
        if (documentChanged)
        {
            _suppress = true;
            Editor.Document = doc.Document;
            _suppress = false;
        }

        if (restoreView && documentChanged)
            RestoreView(doc);

        ApplyGrammar(doc.TitlePath);
        AttachEditor(doc.Tab);
        if (!ReferenceEquals(Documents.SelectedItem, doc.Tab))
        {
            _suppressTabs = true;
            Documents.SelectedItem = doc.Tab;
            _suppressTabs = false;
        }

        doc.Tab.BringIntoView();
        UpdateChrome();
        FollowDocument(doc.TitlePath);
    }

    private void RememberView(OpenDocument doc)
    {
        doc.Caret = Editor.CaretOffset;
        doc.SelectionStart = Editor.SelectionStart;
        doc.SelectionLength = Editor.SelectionLength;
    }

    private void RestoreView(OpenDocument doc)
    {
        var length = doc.Document.TextLength;
        var start = Math.Clamp(doc.SelectionStart, 0, length);
        var count = Math.Clamp(doc.SelectionLength, 0, Math.Max(0, length - start));
        if (count > 0)
            Editor.Select(start, count);
        else
            Editor.CaretOffset = Math.Clamp(doc.Caret, 0, length);
        Editor.TextArea.Caret.BringCaretToView();
    }

    private void AttachEditor(TabItem tab)
    {
        if (ReferenceEquals(EditorHost.Parent, tab))
            return;
        if (EditorHost.Parent is TabItem previous)
            previous.Content = null;
        else if (EditorHost.Parent is Panel panel)
            panel.Children.Remove(EditorHost);
        tab.Content = EditorHost;
    }

    private OpenDocument AddBlankDocument()
    {
        var doc = CreateDocument(new TextDocument(), NextUntitledName());
        _docs.Add(doc);
        _suppressTabs = true;
        Documents.Items.Add(doc.Tab);
        _suppressTabs = false;
        RefreshTab(doc);
        return doc;
    }

    private OpenDocument CreateDocument(TextDocument document, string untitledName)
    {
        var doc = new OpenDocument(document, untitledName);
        doc.CloseButton.Click += (_, e) =>
        {
            e.Handled = true;
            _ = CloseDocumentAsync(doc);
        };
        return doc;
    }

    private void RefreshTab(OpenDocument doc)
    {
        var shown = doc.Dirty ? doc.DisplayName + "*" : doc.DisplayName;
        if (doc.Label.Text != shown)
            doc.Label.Text = shown;
        var tip = string.IsNullOrEmpty(doc.TitlePath) ? doc.DisplayName : doc.TitlePath;
        ToolTip.SetTip(doc.Tab, tip);
        AutomationProperties.SetName(doc.Tab, doc.DisplayName);
        AutomationProperties.SetName(doc.CloseButton, "Close " + doc.DisplayName);
    }

    private OpenDocument? FindDocument(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        try
        {
            if (!Path.IsPathRooted(path))
                return null;
        }
        catch (Exception)
        {
            return null;
        }

        foreach (var doc in _docs)
        {
            if (!string.IsNullOrEmpty(doc.TitlePath) && SamePath(doc.TitlePath, path))
                return doc;
        }

        return null;
    }

    private string NextUntitledName()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var doc in _docs)
        {
            if (string.IsNullOrEmpty(doc.TitlePath))
                used.Add(doc.UntitledName);
        }

        if (used.Add("Untitled"))
            return "Untitled";
        for (var n = 2; ; n++)
        {
            var name = $"Untitled {n}";
            if (used.Add(name))
                return name;
        }
    }

    private static bool IsPristine(OpenDocument doc) =>
        string.IsNullOrEmpty(doc.TitlePath) && !doc.Dirty && doc.Document.TextLength == 0;

    private sealed class LoadedFile
    {
        public LoadedFile(string text, IStorageFile? file, string titlePath)
        {
            Text = text;
            File = file;
            TitlePath = titlePath;
        }

        public string Text { get; }
        public IStorageFile? File { get; }
        public string TitlePath { get; }
    }

    private sealed class OpenDocument
    {
        public OpenDocument(TextDocument document, string untitledName)
        {
            Document = document;
            UntitledName = untitledName;
            Label = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 180,
            };
            CloseButton = new Button
            {
                Content = "×",
                Width = 16,
                Height = 16,
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(6, 0, 0, 0),
                FontSize = 14,
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Center,
                Classes = { "flat" },
            };
            Tab = new TabItem
            {
                Header = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { Label, CloseButton },
                },
                Tag = this,
            };
        }

        public TextDocument Document { get; set; }
        public string UntitledName { get; }
        public string TitlePath { get; set; } = "";
        public IStorageFile? File { get; set; }
        public bool Dirty { get; set; }
        public int LoadGeneration { get; set; }
        public int Caret { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }
        public TabItem Tab { get; }
        public TextBlock Label { get; }
        public Button CloseButton { get; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(TitlePath))
                    return UntitledName;
                try
                {
                    var name = Path.GetFileName(TitlePath);
                    return string.IsNullOrEmpty(name) ? UntitledName : name;
                }
                catch (Exception)
                {
                    return UntitledName;
                }
            }
        }
    }
}
