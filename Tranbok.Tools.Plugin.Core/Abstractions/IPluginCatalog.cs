namespace Tranbok.Tools.Plugin.Core.Abstractions;

public interface IPluginCatalog
{
    IReadOnlyCollection<PluginManifest> Items { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
