using System.Reflection;
using System.Runtime.Loader;
using Tranbok.Tools.Plugin.Core.Enums;
using Tranbok.Tools.Core.Constants;
using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public sealed class PluginDiscoveryService : IPluginDiscoveryService
{
    private readonly IPluginCatalogService _catalog;
    private readonly IServiceProvider _services;
    private readonly HostEnvironmentInfo _hostEnvironment;

    public PluginDiscoveryService(IPluginCatalogService catalog, IServiceProvider services)
    {
        _catalog = catalog;
        _services = services;
        _hostEnvironment = new HostEnvironmentInfo(
            ToolHostConstants.HostName,
            ToolHostConstants.HostVersion,
            Environment.Version.ToString(),
            "net10.0",
            OperatingSystem.IsWindows() ? "Windows" : Environment.OSVersion.Platform.ToString(),
            ToolHostConstants.PluginApiVersion);
    }

    public async ValueTask<PluginDiscoveryResult> LoadAsync(string pluginsDirectory, CancellationToken cancellationToken = default)
    {
        var loadedPlugins = new List<LoadedPluginDescriptor>();
        var errors = new List<PluginLoadError>();

        if (!Directory.Exists(pluginsDirectory))
            return new PluginDiscoveryResult(loadedPlugins, errors);

        foreach (var assemblyPath in Directory.EnumerateFiles(pluginsDirectory, ToolHostConstants.PluginAssemblySearchPattern, SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TryLoadAssemblyAsync(assemblyPath, loadedPlugins, errors, cancellationToken);
        }

        return new PluginDiscoveryResult(loadedPlugins, errors);
    }

    private async ValueTask TryLoadAssemblyAsync(
        string assemblyPath,
        ICollection<LoadedPluginDescriptor> loadedPlugins,
        ICollection<PluginLoadError> errors,
        CancellationToken cancellationToken)
    {
        Assembly assembly;
        try
        {
            assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            errors.Add(new PluginLoadError(assemblyPath, ex.Message, Exception: ex));
            return;
        }

        Type[] pluginTypes;
        try
        {
            pluginTypes = assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(IPlugin).IsAssignableFrom(type))
                .ToArray();
        }
        catch (ReflectionTypeLoadException ex)
        {
            pluginTypes = ex.Types
                .Where(type => type is not null && !type.IsAbstract && typeof(IPlugin).IsAssignableFrom(type))
                .Cast<Type>()
                .ToArray();

            if (pluginTypes.Length == 0)
            {
                errors.Add(new PluginLoadError(assemblyPath, ex.Message, Exception: ex));
                return;
            }
        }

        foreach (var pluginType in pluginTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TryLoadPluginAsync(pluginType, assemblyPath, loadedPlugins, errors, cancellationToken);
        }
    }

    private async ValueTask TryLoadPluginAsync(
        Type pluginType,
        string assemblyPath,
        ICollection<LoadedPluginDescriptor> loadedPlugins,
        ICollection<PluginLoadError> errors,
        CancellationToken cancellationToken)
    {
        IPlugin? plugin = null;
        try
        {
            plugin = Activator.CreateInstance(pluginType) as IPlugin;
            if (plugin is null)
                return;

            if (_catalog.Get(plugin.Descriptor.Id) is not null)
            {
                await plugin.DisposeAsync();
                return;
            }

            var pluginDirectory = Path.GetDirectoryName(assemblyPath) ?? AppContext.BaseDirectory;
            var context = new PluginContext(
                plugin.Descriptor.Id,
                pluginDirectory,
                _services,
                _hostEnvironment,
                PluginIsolationMode.AssemblyLoadContext,
                new Dictionary<string, object?>());

            await plugin.InitializeAsync(context, cancellationToken);
            await plugin.StartAsync(cancellationToken);
            _catalog.Register(plugin, true, _catalog.Plugins.Count);

            var entry = _catalog.Get(plugin.Descriptor.Id);
            if (entry is not null)
                loadedPlugins.Add(new LoadedPluginDescriptor(entry, assemblyPath, pluginDirectory));
        }
        catch (Exception ex)
        {
            if (plugin is not null)
                await plugin.DisposeAsync();
            errors.Add(new PluginLoadError(assemblyPath, ex.Message, plugin?.Descriptor.Id, ex));
        }
    }
}
