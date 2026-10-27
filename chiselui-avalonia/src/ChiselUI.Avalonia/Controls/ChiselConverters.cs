using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ChiselUI.Avalonia.Controls;

public static class ChiselConverters
{
    /// <summary>TreeViewItem.Level → left margin (16px per level, like the web tree).</summary>
    public static readonly IValueConverter TreeIndent = new FuncValueConverter<int, Thickness>(l => new Thickness(Math.Max(0, l) * 16, 0, 0, 0));
}
