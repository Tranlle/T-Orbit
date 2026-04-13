namespace Tranbok.Tools.Core.Models;

public sealed record PluginDiscoveryResult(
    IReadOnlyList<LoadedPluginDescriptor> LoadedPlugins,
    IReadOnlyList<PluginLoadError> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}
