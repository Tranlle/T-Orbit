namespace Tranbok.Tools.Plugin.Core.Abstractions;

public interface IPluginManager
{
    IReadOnlyCollection<PluginHandle> LoadedPlugins { get; }

    Task<IReadOnlyCollection<PluginManifest>> DiscoverAsync(CancellationToken cancellationToken = default);

    Task<PluginHandle> LoadAsync(PluginLoadRequest request, CancellationToken cancellationToken = default);

    Task UnloadAsync(string pluginId, CancellationToken cancellationToken = default);
}
