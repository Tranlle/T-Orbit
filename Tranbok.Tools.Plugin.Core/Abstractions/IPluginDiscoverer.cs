namespace Tranbok.Tools.Plugin.Core.Abstractions;

public interface IPluginDiscoverer
{
    Task<IReadOnlyCollection<PluginManifest>> DiscoverAsync(PluginDiscoveryOptions options, CancellationToken cancellationToken = default);
}
