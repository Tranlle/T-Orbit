using System.Collections.ObjectModel;
using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public sealed class PluginCatalogService : IPluginCatalogService
{
    private readonly ObservableCollection<PluginEntry> _plugins = [];

    public IReadOnlyList<PluginEntry> Plugins => _plugins;
    public IEnumerable<PluginEntry> EnabledPlugins => _plugins.Where(x => x.IsEnabled);

    public void Register(IPlugin plugin, bool enabledByDefault = true, int? sort = null)
    {
        if (_plugins.Any(x => x.Id == plugin.Descriptor.Id))
            throw new InvalidOperationException($"Plugin '{plugin.Descriptor.Id}' is already registered.");

        var entry = new PluginEntry(plugin, enabledByDefault);
        if (sort.HasValue)
            entry.Sort = sort.Value;
        _plugins.Add(entry);
    }

    public PluginEntry? Get(string id) => _plugins.FirstOrDefault(x => x.Id == id);
}
