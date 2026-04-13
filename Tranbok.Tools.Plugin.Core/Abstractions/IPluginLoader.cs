namespace Tranbok.Tools.Plugin.Core.Abstractions;

public interface IPluginLoader
{
    Task<PluginLoadResult> LoadAsync(PluginLoadRequest request, CancellationToken cancellationToken = default);

    Task UnloadAsync(PluginHandle pluginHandle, CancellationToken cancellationToken = default);
}
