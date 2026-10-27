using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TextPlus2;

public sealed class FileTreeNode : INotifyPropertyChanged
{
    private bool _expanded;

    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }
    public bool IsPlaceholder { get; }
    public bool Loaded { get; set; }
    public ObservableCollection<FileTreeNode> Children { get; } = new();

    public bool IsExpanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
                return;
            _expanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FileTreeNode(string name, string fullPath, bool isDirectory)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        // ItemCount 0 hides the twisty. Keep a stand-in until the folder is listed.
        if (isDirectory)
            Children.Add(CreatePlaceholder());
    }

    private FileTreeNode()
    {
        Name = "";
        FullPath = "";
        IsPlaceholder = true;
    }

    public static FileTreeNode CreatePlaceholder() => new();
}

public partial class MainWindow
{
    private enum LoadResult { Opened, Same, Failed }

    private readonly ObservableCollection<FileTreeNode> _roots = new();
    private string? _treeDirectory;
    private string? _pendingSelect;
    private double _treeWidth = 220;
    private int _treeGeneration;
    private int _selectAttempts;
    private int _treeOpen;
    private bool _treeDrives;
    private bool _treeRootChosen;
    private bool _fileTreeVisible = true;
    private bool _followOpenFile;
    private int _reveal;
    private bool _expandSnapshot;
    private bool _wasExpanded;

    private void InitFileTree()
    {
        Files.ItemsSource = _roots;
        Files.AddHandler(TreeViewItem.ExpandedEvent, OnFileExpanded);
        Files.AddHandler(InputElement.PointerPressedEvent, OnTreePointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        Files.AddHandler(InputElement.DoubleTappedEvent, OnTreeDoubleTappedTunnel, RoutingStrategies.Tunnel);
        Files.AddHandler(InputElement.DoubleTappedEvent, OnTreeDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);
        Files.AddHandler(InputElement.KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Bubble);
        Loaded += (_, _) =>
        {
            if (_treeRootChosen)
            {
                ScheduleSelect(_pendingSelect);
                return;
            }

            _treeRootChosen = true;
            ShowDrives();
        };
    }

    private void FileTree_Click(object? sender, RoutedEventArgs e)
    {
        // A pointer click flips IsChecked before this runs. The Ctrl+B hotkey only raises Click.
        ApplyFileTreeVisible(!_fileTreeVisible);
        FileTreeMenuItem.IsChecked = _fileTreeVisible;
    }

    private void FollowOpenFile_Click(object? sender, RoutedEventArgs e)
    {
        // A pointer click flips IsChecked before this runs. Assign the flag we own.
        _followOpenFile = !_followOpenFile;
        FollowOpenFileMenuItem.IsChecked = _followOpenFile;
        if (_followOpenFile)
            FollowDocument(TitlePath);
    }

    private void ApplyFileTreeVisible(bool visible)
    {
        _fileTreeVisible = visible;
        var columns = Workspace.ColumnDefinitions;
        if (!visible)
        {
            if (columns[0].ActualWidth >= 80)
                _treeWidth = columns[0].ActualWidth;
            columns[0].MinWidth = 0;
            columns[0].Width = new GridLength(0);
            columns[1].Width = new GridLength(0);
            FileTreeHost.IsVisible = false;
            FileTreeSplitter.IsVisible = false;
            FocusEditor();
            return;
        }

        columns[0].MinWidth = 120;
        columns[0].Width = new GridLength(Math.Max(120, _treeWidth));
        columns[1].Width = new GridLength(6);
        FileTreeHost.IsVisible = true;
        FileTreeSplitter.IsVisible = true;
    }

    public async void OpenStartupFile(string path)
    {
        // Claim the root before the first await so Loaded does not flash the drive list.
        _treeRootChosen = true;
        try
        {
            if (Directory.Exists(path) && !File.Exists(path))
            {
                ShowDirectory(path, null);
                return;
            }
        }
        catch (Exception)
        {
            ShowDrives();
            return;
        }

        TrySetTreeTitle(path);
        var loaded = await ReadForOpenAsync(path, null);
        if (loaded == null)
            ShowDrives();
        else
            CommitLoaded(loaded, show: true);
    }

    private async Task OpenAsync()
    {
        var files = await AfterModalAsync(() => StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open",
            AllowMultiple = true,
            FileTypeFilter = [TextType, FilePickerFileTypes.All],
        }));
        if (files.Count == 0)
        {
            SetStatus("Open canceled");
            FocusEditor();
            return;
        }

        var opened = false;
        var failed = false;
        foreach (var file in files)
        {
            var loaded = await ReadForOpenAsync(LocalPathOf(file), file);
            if (loaded == null)
            {
                failed = true;
                continue;
            }

            if (CommitLoaded(loaded, show: true) == LoadResult.Opened)
                opened = true;
        }

        if (failed)
            SetStatus("Could not open file");
        else if (opened)
            SetStatus("Opened");
        FocusEditor();
    }

    private async Task OpenFolderAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Open Folder",
            AllowMultiple = false,
        };

        if (!_treeDrives && !string.IsNullOrEmpty(_treeDirectory) && StorageProvider.CanOpen)
        {
            try
            {
                var start = await StorageProvider.TryGetFolderFromPathAsync(_treeDirectory);
                if (start != null)
                    options.SuggestedStartLocation = start;
            }
            catch (Exception)
            {
                // The picker still opens at its default location.
            }
        }

        var folders = await AfterModalAsync(() => StorageProvider.OpenFolderPickerAsync(options));
        if (folders.Count == 0)
        {
            SetStatus("Open folder canceled");
            FocusEditor();
            return;
        }

        var path = LocalPathOf(folders[0]);
        if (string.IsNullOrEmpty(path))
        {
            SetStatus("Could not open folder");
            FocusEditor();
            return;
        }

        try
        {
            if (!Directory.Exists(path))
            {
                SetStatus("Could not open folder");
                FocusEditor();
                return;
            }
        }
        catch (Exception)
        {
            SetStatus("Could not open folder");
            FocusEditor();
            return;
        }

        ShowDirectory(path, null);
        if (!string.IsNullOrEmpty(TitlePath) && IsInsideDirectory(path, TitlePath))
            RequestReveal(TitlePath);
        SetStatus("Opened folder");
        FocusEditor();
    }

    private void FollowDocument(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Files.SelectedItem = null;
            return;
        }

        string? directory;
        try
        {
            if (!Path.IsPathRooted(path))
                return;
            directory = Path.GetDirectoryName(path);
        }
        catch (Exception)
        {
            return;
        }

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return;

        _treeRootChosen = true;
        if (_followOpenFile)
        {
            if (_treeDrives || _treeDirectory == null || !SameDirectory(_treeDirectory, directory) || !ContainsFile(path))
                ShowDirectory(directory, path);
            else
                ScheduleSelect(path);
            return;
        }

        // A chosen folder stays the root. A file under it is revealed in place.
        // The drive list is not a folder yet, so the first opened file still picks its parent.
        if (!_treeDrives && _treeDirectory != null && IsInsideDirectory(_treeDirectory, path))
        {
            if (ContainsFile(path))
                ScheduleSelect(path);
            else
                RequestReveal(path);
            return;
        }

        if (_treeDrives || _treeDirectory == null)
            ShowDirectory(directory, path);
    }

    private void ShowDrives()
    {
        _treeGeneration++;
        _treeDrives = true;
        _treeDirectory = null;
        _pendingSelect = null;
        SetTreeTitle("Drives", null);
        _roots.Clear();

        if (!OperatingSystem.IsWindows())
        {
            SetTreeTitle("/", "/");
            _roots.Add(new FileTreeNode("/", "/", isDirectory: true));
            return;
        }

        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (Exception)
        {
            drives = [];
        }

        foreach (var drive in drives)
        {
            // IsReady on a network root can sit there. Skip those before asking.
            if (drive.DriveType is DriveType.Network or DriveType.NoRootDirectory)
                continue;
            try
            {
                if (!drive.IsReady)
                    continue;
            }
            catch (Exception)
            {
                continue;
            }

            var root = drive.RootDirectory.FullName;
            if (string.IsNullOrEmpty(root))
                continue;
            _roots.Add(new FileTreeNode(root, root, isDirectory: true));
        }

        if (_roots.Count == 0)
            _roots.Add(new FileTreeNode(@"C:\", @"C:\", isDirectory: true));
    }

    private void ShowDirectory(string directory, string? selectPath)
    {
        string key;
        try
        {
            key = DirectoryKey(directory);
        }
        catch (Exception)
        {
            return;
        }

        _treeGeneration++;
        _treeDrives = false;
        _treeDirectory = key;
        var title = DirectoryTitle(key);
        SetTreeTitle(title, key);

        List<FileTreeNode> children;
        try
        {
            children = ListDirectory(key);
        }
        catch (Exception)
        {
            children = [];
            SetStatus("Cannot list folder");
        }

        _roots.Clear();
        foreach (var child in children)
            _roots.Add(child);
        ScheduleSelect(selectPath);
    }

    private void SetTreeTitle(string text, string? tip)
    {
        FileTreeTitle.Text = text;
        AutomationProperties.SetName(FileTreeTitle, text);
        AutomationProperties.SetName(FileTreeHost, text);
        ToolTip.SetTip(FileTreeTitle, tip);
    }

    private void TrySetTreeTitle(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(directory))
                return;
            SetTreeTitle(DirectoryTitle(directory), DirectoryKey(directory));
        }
        catch (Exception)
        {
            // The load reports its own failure.
        }
    }

    private async void OnFileExpanded(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not TreeViewItem item)
            return;
        if (item.DataContext is not FileTreeNode node || !node.IsDirectory || node.Loaded)
            return;

        await LoadChildrenAsync(node);
    }

    private async Task<bool> LoadChildrenAsync(FileTreeNode node)
    {
        if (!node.IsDirectory || node.IsPlaceholder)
            return false;
        if (node.Loaded)
            return await WaitForChildrenAsync(node);

        node.Loaded = true;
        var generation = _treeGeneration;
        List<FileTreeNode> children;
        try
        {
            children = await Task.Run(() => ListDirectory(node.FullPath));
        }
        catch (Exception)
        {
            if (generation == _treeGeneration)
            {
                node.Loaded = false;
                RestorePlaceholder(node);
                SetStatus("Cannot list folder");
            }
            return false;
        }

        if (generation != _treeGeneration)
            return false;

        node.Children.Clear();
        foreach (var child in children)
            node.Children.Add(child);
        return true;
    }

    private async Task<bool> WaitForChildrenAsync(FileTreeNode node)
    {
        for (var i = 0; i < 40; i++)
        {
            if (!HasOnlyPlaceholder(node))
                return true;
            if (!node.Loaded)
                return false;
            await Task.Delay(25);
        }

        return !HasOnlyPlaceholder(node);
    }

    private void OnTreeDoubleTappedTunnel(object? sender, TappedEventArgs e)
    {
        _expandSnapshot = false;
        var item = ItemFromSource(e.Source);
        if (item?.DataContext is not FileTreeNode { IsDirectory: true })
            return;
        _expandSnapshot = true;
        _wasExpanded = item.IsExpanded;
    }

    private void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        var node = NodeFromSource(e.Source);
        if (node == null || node.IsPlaceholder)
            return;
        if (!node.IsDirectory)
            return;

        var item = ItemFromSource(e.Source);
        // The row toggles itself first. If that did not run, expand a collapsed folder.
        if (item is { ItemCount: > 0 } && _expandSnapshot && !_wasExpanded && !item.IsExpanded)
            item.IsExpanded = true;
        else if (item is { ItemCount: > 0, IsExpanded: false } && !_expandSnapshot)
            item.IsExpanded = true;
    }

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // A single left click opens a file. Folders still expand from the twisty or a double-click.
        if (e.ClickCount != 1 || !e.GetCurrentPoint(Files).Properties.IsLeftButtonPressed)
            return;
        var node = NodeFromSource(e.Source);
        if (node == null || node.IsPlaceholder || node.IsDirectory)
            return;
        _ = OpenTreeFileAsync(node);
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Files.SelectedItem is not FileTreeNode node || node.IsDirectory || node.IsPlaceholder)
            return;
        e.Handled = true;
        _ = OpenTreeFileAsync(node);
    }

    private async Task OpenTreeFileAsync(FileTreeNode node)
    {
        var ticket = ++_treeOpen;
        if (FindDocument(node.FullPath) is { } open)
        {
            Activate(open, restoreView: true);
            FocusEditor();
            return;
        }

        var loaded = await ReadForOpenAsync(node.FullPath, null);
        if (loaded == null)
        {
            if (ticket == _treeOpen)
            {
                SetStatus("Could not open file");
                ReselectCurrent();
                FocusEditor();
            }
            return;
        }

        CommitLoaded(loaded, show: ticket == _treeOpen);
        if (ticket == _treeOpen)
            FocusEditor();
    }

    private void ScheduleSelect(string? path)
    {
        _pendingSelect = path;
        _selectAttempts = 0;
        if (string.IsNullOrEmpty(path) || _treeDrives)
            return;
        Dispatcher.UIThread.Post(ApplyPendingSelect, DispatcherPriority.Loaded);
    }

    private void ApplyPendingSelect()
    {
        var path = _pendingSelect;
        if (string.IsNullOrEmpty(path) || _treeDrives)
            return;

        FileTreeNode? match = null;
        foreach (var node in _roots)
        {
            if (node.IsDirectory || node.IsPlaceholder)
                continue;
            if (SamePath(node.FullPath, path))
            {
                match = node;
                break;
            }
        }

        if (match == null)
        {
            if (++_selectAttempts < 8)
                Dispatcher.UIThread.Post(ApplyPendingSelect, DispatcherPriority.Background);
            return;
        }

        Files.SelectedItem = match;
        if (Files.ContainerFromItem(match) is TreeViewItem item)
            item.BringIntoView();
        else if (++_selectAttempts < 8)
            Dispatcher.UIThread.Post(ApplyPendingSelect, DispatcherPriority.Background);
    }

    private void ReselectCurrent()
    {
        if (string.IsNullOrEmpty(TitlePath) || _treeDrives || _treeDirectory == null)
        {
            Files.SelectedItem = null;
            return;
        }

        if (ContainsFile(TitlePath))
        {
            ScheduleSelect(TitlePath);
            ApplyPendingSelect();
            return;
        }

        if (IsInsideDirectory(_treeDirectory, TitlePath))
        {
            RequestReveal(TitlePath);
            return;
        }

        Files.SelectedItem = null;
    }

    private void RequestReveal(string path)
    {
        var ticket = ++_reveal;
        var generation = _treeGeneration;
        _ = RevealAsync(path, ticket, generation);
    }

    private async Task RevealAsync(string path, int ticket, int generation)
    {
        if (ticket != _reveal || generation != _treeGeneration)
            return;
        if (_treeDrives || string.IsNullOrEmpty(_treeDirectory) || !IsInsideDirectory(_treeDirectory, path))
            return;

        string[] parts;
        try
        {
            var relative = Path.GetRelativePath(_treeDirectory, Path.GetFullPath(path));
            parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception)
        {
            return;
        }

        if (parts.Length == 0 || parts[0] == "..")
            return;

        IReadOnlyList<FileTreeNode> level = _roots;
        FileTreeNode? node = null;
        for (var i = 0; i < parts.Length; i++)
        {
            if (ticket != _reveal || generation != _treeGeneration)
                return;

            node = FindNamed(level, parts[i]);
            if (node == null)
                return;

            if (i == parts.Length - 1)
                break;
            if (!node.IsDirectory || !await LoadChildrenAsync(node))
                return;

            node.IsExpanded = true;
            RealizeExpanded(node);
            level = node.Children;
        }

        if (node == null)
            return;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (ticket != _reveal || generation != _treeGeneration)
                return;
            if (Files.TreeContainerFromItem(node) is TreeViewItem item)
            {
                Files.SelectedItem = node;
                item.BringIntoView();
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Loaded);
        }
    }

    private void RealizeExpanded(FileTreeNode node)
    {
        if (Files.TreeContainerFromItem(node) is not TreeViewItem item)
            return;
        if (!item.IsExpanded)
            item.IsExpanded = true;
    }

    private static FileTreeNode? FindNamed(IReadOnlyList<FileTreeNode> nodes, string name)
    {
        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (var node in nodes)
        {
            if (node.IsPlaceholder)
                continue;
            if (string.Equals(node.Name, name, cmp))
                return node;
        }

        return null;
    }

    private static bool HasOnlyPlaceholder(FileTreeNode node) =>
        node.Children.Count == 1 && node.Children[0].IsPlaceholder;

    private static bool IsInsideDirectory(string directory, string path)
    {
        try
        {
            var root = DirectoryKey(directory);
            var full = Path.GetFullPath(path);
            var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return full.StartsWith(root, cmp);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ContainsFile(string path)
    {
        foreach (var node in _roots)
        {
            if (!node.IsDirectory && !node.IsPlaceholder && SamePath(node.FullPath, path))
                return true;
        }
        return false;
    }

    private static void RestorePlaceholder(FileTreeNode node)
    {
        if (node.Children.Count == 1 && node.Children[0].IsPlaceholder)
            return;
        node.Children.Clear();
        node.Children.Add(FileTreeNode.CreatePlaceholder());
    }

    private static TreeViewItem? ItemFromSource(object? source)
    {
        if (source is not Visual visual)
            return null;
        return visual as TreeViewItem ?? visual.FindAncestorOfType<TreeViewItem>();
    }

    private static FileTreeNode? NodeFromSource(object? source) =>
        ItemFromSource(source)?.DataContext as FileTreeNode;

    private static List<FileTreeNode> ListDirectory(string path)
    {
        var dirs = new List<FileTreeNode>();
        var files = new List<FileTreeNode>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(path))
        {
            try
            {
                var attr = File.GetAttributes(entry);
                if ((attr & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;
                var name = Path.GetFileName(entry.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(name))
                    continue;
                var isDir = (attr & FileAttributes.Directory) != 0;
                (isDir ? dirs : files).Add(new FileTreeNode(name, entry, isDir));
            }
            catch (Exception)
            {
                // One unreadable entry should not blank the folder.
            }
        }

        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        dirs.Sort((a, b) => comparer.Compare(a.Name, b.Name));
        files.Sort((a, b) => comparer.Compare(a.Name, b.Name));
        dirs.AddRange(files);
        return dirs;
    }

    private static string DirectoryKey(string path)
    {
        var full = Path.GetFullPath(path);
        if (full.Length == 0)
            return full;
        var last = full[^1];
        if (last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar)
            return full;
        return full + Path.DirectorySeparatorChar;
    }

    private static string DirectoryTitle(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        if (!string.IsNullOrEmpty(root) && SameDirectory(full, root))
            return root;
        var trimmed = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(name) ? full : name;
    }

    private static bool SameDirectory(string a, string b)
    {
        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(DirectoryKey(a), DirectoryKey(b), cmp);
    }

    private static bool SamePath(string a, string b)
    {
        try
        {
            var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), cmp);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string? LocalPathOf(IStorageItem file)
    {
        try
        {
            var local = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(local))
                return local;
            if (file.Path.IsAbsoluteUri)
                return file.Path.LocalPath;
        }
        catch (Exception)
        {
            return null;
        }
        return null;
    }
}
