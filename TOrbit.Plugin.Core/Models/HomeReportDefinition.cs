namespace TOrbit.Plugin.Core.Models;

public sealed class HomeReportDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public int Sort { get; init; }
    public string SourcePluginId { get; init; } = string.Empty;
    public bool IsBuiltIn { get; init; }
    public Func<CancellationToken, ValueTask<object>>? ViewFactory { get; init; }
}
