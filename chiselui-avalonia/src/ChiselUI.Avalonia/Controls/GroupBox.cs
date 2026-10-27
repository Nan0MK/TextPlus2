using System;
using Avalonia.Controls.Primitives;

namespace ChiselUI.Avalonia.Controls;

/// <summary>Raised sub-panel with an accent caption (no etched line). Add class "well" for the concave variant.</summary>
public class GroupBox : HeaderedContentControl
{
    protected override Type StyleKeyOverride => typeof(GroupBox);
}
