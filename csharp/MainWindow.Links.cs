using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace TextPlus2;

public partial class MainWindow
{
    private static readonly Cursor LinkHand = new(StandardCursorType.Hand);
    private static readonly TextDecorationCollection LinkUnderline = TextDecorations.Underline;

    private MarkdownLinkColorizer _linkColorizer = null!;
    private MarkdownIndex? _linkIndex;
    private AvaloniaEdit.Document.TextDocument? _linkDocument;
    private bool _linksDirty = true;
    private bool _followingLink;
    private bool _linkHint;
    private bool _linkHand;

    private bool IsMarkdownDocument => MarkdownIndex.IsMarkdownExtension(ExtensionKey(TitlePath));

    private void InitLinks()
    {
        _linkColorizer = new MarkdownLinkColorizer(this);
        Editor.TextArea.TextView.LineTransformers.Add(_linkColorizer);
        Editor.TextArea.TextView.AddHandler(PointerPressedEvent, OnLinkPressed, RoutingStrategies.Tunnel);
        Editor.TextArea.TextView.AddHandler(PointerMovedEvent, OnLinkMoved, RoutingStrategies.Tunnel);
        Editor.TextArea.TextView.PointerExited += (_, _) => ShowLinkHover(null, false);
        AddHandler(KeyDownEvent, OnLinkKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnLinkKeyUp, RoutingStrategies.Tunnel);
    }

    private void OnLinkKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.LeftCtrl or Key.RightCtrl) || !_linkHint || _linkHand)
            return;
        Editor.TextArea.TextView.Cursor = LinkHand;
        _linkHand = true;
    }

    private void OnLinkKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.LeftCtrl or Key.RightCtrl) || !_linkHand)
            return;
        Editor.TextArea.TextView.Cursor = null;
        _linkHand = false;
    }

    private void OnLinkMoved(object? sender, PointerEventArgs e)
    {
        if (!IsMarkdownDocument)
        {
            ShowLinkHover(null, false);
            return;
        }

        ShowLinkHover(LinkAt(e), e.KeyModifiers.HasFlag(KeyModifiers.Control));
    }

    private void OnLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsMarkdownDocument || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;
        if (!e.GetCurrentPoint(Editor.TextArea.TextView).Properties.IsLeftButtonPressed)
            return;
        if (LinkAt(e) is not { } link)
            return;

        e.Handled = true;
        _ = FollowLinkAsync(link.Destination);
    }

    private MarkdownLink? LinkAt(PointerEventArgs e)
    {
        var view = Editor.TextArea.TextView;
        var document = Editor.Document;
        if (document == null)
            return null;

        var position = view.GetPositionFloor(e.GetPosition(view) + view.ScrollOffset);
        if (position == null)
            return null;

        try
        {
            var offset = document.GetOffset(position.Value.Location);
            return CurrentLinks(document).At(offset);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private MarkdownIndex CurrentLinks(AvaloniaEdit.Document.TextDocument document)
    {
        if (_linkIndex == null || _linksDirty || !ReferenceEquals(_linkDocument, document))
        {
            _linkIndex = MarkdownIndex.Build(document.Text);
            _linkDocument = document;
            _linksDirty = false;
        }

        return _linkIndex;
    }

    private void ShowLinkHover(MarkdownLink? link, bool control)
    {
        if (link == null)
        {
            if (_linkHint)
            {
                StatusText.Text = _message;
                _linkHint = false;
            }

            if (_linkHand)
            {
                Editor.TextArea.TextView.Cursor = null;
                _linkHand = false;
            }

            return;
        }

        StatusText.Text = link.Value.Destination;
        _linkHint = true;
        if (control == _linkHand)
            return;
        Editor.TextArea.TextView.Cursor = control ? LinkHand : null;
        _linkHand = control;
    }

    private async Task FollowLinkAsync(string destination)
    {
        if (_followingLink)
            return;
        _followingLink = true;
        try
        {
            var target = MarkdownIndex.Classify(destination);
            switch (target.Kind)
            {
                case LinkTargetKind.Web:
                    OpenWebLink(target.Path);
                    break;
                case LinkTargetKind.Anchor:
                    OpenAnchor(target.Fragment);
                    break;
                case LinkTargetKind.Local:
                    await OpenLocalLinkAsync(target);
                    break;
                default:
                    SetStatus("Could not open link");
                    break;
            }
        }
        finally
        {
            _followingLink = false;
            FocusEditor();
        }
    }

    private void OpenWebLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            SetStatus("Opened link");
        }
        catch (Exception)
        {
            SetStatus("Could not open link");
        }
    }

    private void OpenAnchor(string? fragment)
    {
        if (fragment != null && MarkdownIndex.FindHeading(Editor.Document.Text, fragment) is int offset)
        {
            PlaceCaret(offset, extend: false);
            SetStatus("Opened link");
            return;
        }

        SetStatus("Link target was not found");
    }

    private async Task OpenLocalLinkAsync(LinkTarget target)
    {
        if (!TryResolveLocal(target.Path, out var full))
            return;

        try
        {
            if (Directory.Exists(full))
            {
                _treeRootChosen = true;
                ShowDirectory(full, null);
                SetStatus("Opened folder");
                return;
            }
        }
        catch (Exception)
        {
            SetStatus("Could not open link");
            return;
        }

        if (!string.IsNullOrEmpty(TitlePath) && SamePath(full, TitlePath))
        {
            if (target.Fragment == null)
                SetStatus("Opened link");
            else
                OpenAnchor(target.Fragment);
            return;
        }

        var loaded = await ReadForOpenAsync(full, null);
        if (loaded == null)
        {
            SetStatus("Could not open link");
            return;
        }

        CommitLoaded(loaded, show: true);
        if (string.IsNullOrEmpty(target.Fragment))
        {
            SetStatus("Opened link");
            return;
        }

        if (MarkdownIndex.FindHeading(Editor.Document.Text, target.Fragment) is int heading)
        {
            PlaceCaret(heading, extend: false);
            SetStatus("Opened link");
        }
        else
        {
            SetStatus("Heading was not found");
        }
    }

    private bool TryResolveLocal(string path, out string full)
    {
        full = "";
        try
        {
            string combined;
            if (Path.IsPathRooted(path))
            {
                combined = Path.GetFullPath(path);
            }
            else if (string.IsNullOrEmpty(TitlePath))
            {
                SetStatus("Save the file before opening a relative link");
                return false;
            }
            else
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(TitlePath));
                if (string.IsNullOrEmpty(directory))
                {
                    SetStatus("Could not open link");
                    return false;
                }

                combined = Path.GetFullPath(Path.Combine(directory, path));
            }

            if (Directory.Exists(combined) || File.Exists(combined))
            {
                full = combined;
                return true;
            }

            // [[Note]] has no extension. The file beside this one is Note.md.
            if (!Path.HasExtension(combined))
            {
                foreach (var ext in new[] { ".md", ".markdown", ".mdown", ".mkd" })
                {
                    var candidate = combined + ext;
                    if (File.Exists(candidate))
                    {
                        full = candidate;
                        return true;
                    }
                }
            }

            SetStatus("Link target was not found");
            return false;
        }
        catch (Exception)
        {
            SetStatus("Could not open link");
            return false;
        }
    }

    private void ColorizeMarkdownLinks(DocumentLine line)
    {
        if (!IsMarkdownDocument || Editor.Document == null)
            return;

        var index = CurrentLinks(Editor.Document);
        var start = line.Offset;
        var end = line.EndOffset;
        index.Visit(start, end, link =>
        {
            var from = Math.Max(link.Start, start);
            var to = Math.Min(link.End, end);
            if (from < to)
                _linkColorizer.Paint(from, to);
        });
    }

    private sealed class MarkdownLinkColorizer : DocumentColorizingTransformer
    {
        private readonly MainWindow _window;

        public MarkdownLinkColorizer(MainWindow window) => _window = window;

        public void Paint(int start, int end) =>
            ChangeLinePart(start, end, element => element.TextRunProperties.SetTextDecorations(LinkUnderline));

        protected override void ColorizeLine(DocumentLine line) => _window.ColorizeMarkdownLinks(line);
    }
}
