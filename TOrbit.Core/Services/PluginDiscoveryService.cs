using TOrbit.Core.Models;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;

namespace TOrbit.Core.Services;

public sealed class PluginDiscoveryService : IPluginDiscoveryService
{
    private readonly IPluginCatalogService _catalog;
    private readonly IPluginDiscoverer _discoverer;
    private readonly IPluginDependencyResolver _dependencyResolver;
    private readonly IPluginLoader _loader;
    private readonly IPluginVariableService _variableService;

    public PluginDiscoveryService(
        IPluginCatalogService catalog,
        IPluginDiscoverer discoverer,
        IPluginDependencyResolver dependencyResolver,
        IPluginLoader loader,
        IPluginVariableService variableService)
    {
        _catalog = catalog;
        _discoverer = discoverer;
        _dependencyResolver = dependencyResolver;
        _loader = loader;
        _variableService = variableService;
    }

    public async ValueTask<PluginDiscoveryResult> LoadAsync(string pluginsDirectory, CancellationToken cancellationToken = default)
    {
        var loadedPlugins = new List<LoadedPluginDescriptor>();
        var errors = new List<PluginLoadError>();

        if (!Directory.Exists(pluginsDirectory))
            return new PluginDiscoveryResult(loadedPlugins, errors);

        IReadOnlyCollection<PluginManifest> manifests;
        try
        {
            manifests = await _discoverer.DiscoverAsync(
                new PluginDiscoveryOptions(pluginsDirectory),
                cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add(new PluginLoadError(pluginsDirectory, ex.Message, Exception: ex));
            return new PluginDiscoveryResult(loadedPlugins, errors);
        }

        PluginDependencyGraph dependencyGraph;
        try
        {
            dependencyGraph = await _dependencyResolver.ResolveAsync(manifests, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add(new PluginLoadError(pluginsDirectory, ex.Message, Exception: ex));
            return new PluginDiscoveryResult(loadedPlugins, errors);
        }

        var manifestById = manifests.ToDictionary(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var pluginId in dependencyGraph.LoadOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!manifestById.TryGetValue(pluginId, out var manifest))
                continue;

            await TryLoadManifestAsync(manifest, loadedPlugins, errors, cancellationToken);
        }

        return new PluginDiscoveryResult(loadedPlugins, errors);
    }

    private async ValueTask TryLoadManifestAsync(
        PluginManifest manifest,
        ICollection<LoadedPluginDescriptor> loadedPlugins,
        ICollection<PluginLoadError> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_catalog.Get(manifest.Id) is not null)
                return;

            var loadResult = await _loader.LoadAsync(new PluginLoadRequest(manifest), cancellationToken);
            _catalog.Register(loadResult.Handle.Instance, true, _catalog.Plugins.Count);
            _variableService.InjectOne(loadResult.Handle.Instance);

            var entry = _catalog.Get(manifest.Id);
            if (entry is not null)
            {
                var assemblyPath = Path.GetFullPath(Path.Combine(manifest.BaseDirectory, manifest.Descriptor.EntryAssembly));
                loadedPlugins.Add(new LoadedPluginDescriptor(entry, assemblyPath, manifest.BaseDirectory));
            }
        }
        catch (Exception ex)
        {
            var assemblyPath = Path.GetFullPath(Path.Combine(manifest.BaseDirectory, manifest.Descriptor.EntryAssembly));
            errors.Add(new PluginLoadError(assemblyPath, ex.Message, manifest.Id, ex));
        }
    }
}
