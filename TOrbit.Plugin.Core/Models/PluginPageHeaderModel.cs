namespace TOrbit.Plugin.Core.Models;

public sealed class PluginPageHeaderModel
{
    public string? Context { get; init; }

    public IReadOnlyList<PluginPageHeaderMetric> Metrics { get; init; } = [];

    public IReadOnlyList<PluginPageHeaderBadge> Badges { get; init; } = [];
}

public sealed class PluginPageHeaderMetric
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public PluginPageHeaderTone Tone { get; init; } = PluginPageHeaderTone.Neutral;
}

public sealed class PluginPageHeaderBadge
{
    public string Text { get; init; } = string.Empty;

    public string? Icon { get; init; }

    public PluginPageHeaderTone Tone { get; init; } = PluginPageHeaderTone.Neutral;

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);
}

public enum PluginPageHeaderTone
{
    Neutral,
    Accent,
    Success,
    Warning,
    Danger
}
