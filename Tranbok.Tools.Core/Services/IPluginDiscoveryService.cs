using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public interface IPluginDiscoveryService
{
    ValueTask<PluginDiscoveryResult> LoadAsync(string pluginsDirectory, CancellationToken cancellationToken = default);
}
