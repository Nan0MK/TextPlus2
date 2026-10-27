using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChiselUI.Demo;

public record FileItem(string Name, string Sub, string Icon, string Type, string Size, string Modified);

public partial class ExplorerView : UserControl
{
    public static readonly List<FileItem> Items = new()
    {
        new("Projects", "14 items", "folder-code", "File folder", "", "10/24/2026 9:12 AM"),
        new("Photos", "238 items", "folder-photo", "File folder", "", "10/20/2026 7:40 PM"),
        new("Music", "61 items", "folder-music", "File folder", "", "9/30/2026 11:02 AM"),
        new("Taxes 2025", "9 items", "folder-doc", "File folder", "", "4/12/2026 3:18 PM"),
        new("Résumé – Jake.docx", "47 KB", "word", "Word document", "47 KB", "10/02/2026 2:25 PM"),
        new("Budget 2026.xlsx", "112 KB", "xls", "Excel worksheet", "112 KB", "10/18/2026 8:03 PM"),
        new("Lake trip.jpg", "2.4 MB", "photo", "JPEG image", "2.4 MB", "8/14/2026 6:42 PM"),
        new("Sunset ridge.png", "5.1 MB", "sunset", "PNG image", "5.1 MB", "8/15/2026 8:10 PM"),
        new("Night drive.mp3", "4:12", "cd", "MP3 audio", "6.0 MB", "7/03/2026 10:55 PM"),
        new("main.cpp", "4 KB", "cpp", "C++ source", "4 KB", "10/25/2026 10:39 PM"),
        new("site-backup.zip", "184 MB", "folder-zip", "Compressed folder", "184 MB", "10/21/2026 1:30 AM"),
        new("Backup (E:)", "412 GB free of 1 TB", "drive-big", "Local disk", "1 TB", ""),
    };

    public ExplorerView()
    {
        InitializeComponent();
        IconList.ItemsSource = Items;
        DetailsGrid.ItemsSource = Items;
        IconList.SelectedIndex = 6;
        DetailsGrid.SelectedIndex = 6;
        NavDocuments.IsSelected = true;
    }

    public void ShowDetails(bool details)
    {
        IconList.IsVisible = !details;
        DetailsGrid.IsVisible = details;
        ViewIcons.IsChecked = !details;
        ViewDetails.IsChecked = details;
    }

    void OnIcons(object? s, RoutedEventArgs e) => ShowDetails(false);
    void OnDetails(object? s, RoutedEventArgs e) => ShowDetails(true);

    void OnSelection(object? s, SelectionChangedEventArgs e)
    {
        var item = (s as ListBox)?.SelectedItem as FileItem ?? (s as DataGrid)?.SelectedItem as FileItem;
        if (item == null) return;
        if (ReferenceEquals(s, IconList) && !ReferenceEquals(DetailsGrid.SelectedItem, item)) DetailsGrid.SelectedItem = item;
        if (ReferenceEquals(s, DetailsGrid) && !ReferenceEquals(IconList.SelectedItem, item)) IconList.SelectedItem = item;
        PreviewName.Text = item.Name;
        PreviewType.Text = PropType.Text = item.Type;
        PropSize.Text = item.Size == "" ? "—" : item.Size;
        PreviewIcon.Kind = item.Icon;
        StatusText.Text = $"{Items.Count} items, 1 selected{(item.Size == "" ? "" : ", " + item.Size)}";
    }
}
