using System;
using FluentIcons.Common;

namespace HPPDonat.Models;

public class NavigationItem
{
    public required string Label { get; set; }
    public required Symbol Icon { get; set; }
    public required Type ViewModelType { get; set; }
    public string ToolTip { get; set; } = "";
}
