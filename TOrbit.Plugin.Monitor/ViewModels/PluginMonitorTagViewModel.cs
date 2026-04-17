using Avalonia.Media;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed class PluginMonitorTagViewModel
{
    public required string Text { get; init; }
    public required IBrush Background { get; init; }
    public required IBrush Foreground { get; init; }
}
