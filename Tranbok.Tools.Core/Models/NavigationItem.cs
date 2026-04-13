namespace Tranbok.Tools.Core.Models;

public sealed partial class NavigationItem : ObservableObject
{
    [ObservableProperty]
    private bool isActive;

    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Icon { get; init; } = string.Empty;
    public int Sort { get; init; }
    public bool IsPinned { get; init; }
    public PluginEntry? Plugin { get; init; }
}
