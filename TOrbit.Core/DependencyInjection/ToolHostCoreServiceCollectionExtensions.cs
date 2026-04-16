using Microsoft.Extensions.DependencyInjection;
using TOrbit.Core.Services;
using TOrbit.Core.Tools;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Tools;

namespace TOrbit.Core.DependencyInjection;

public static class ToolHostCoreServiceCollectionExtensions
{
    public static IServiceCollection AddToolHostCore(this IServiceCollection services)
    {
        // Core infrastructure services.
        services.AddSingleton<IAppDiagnosticsService, AppDiagnosticsService>();
        services.AddSingleton<IPluginValidationStatusService, PluginValidationStatusService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IAppShellService, AppShellService>();
        services.AddSingleton<IAppPreferencesService, AppPreferencesService>();
        services.AddSingleton<IAppStartupCoordinator, AppStartupCoordinator>();
        services.AddSingleton<IAppShutdownCoordinator, AppShutdownCoordinator>();
        services.AddSingleton<IPluginCatalogService, PluginCatalogService>();
        services.AddSingleton<IPluginDiscoveryService, PluginDiscoveryService>();
        services.AddSingleton<IPluginVariableService, PluginVariableService>();
        services.AddSingleton<IKeyMapService, KeyMapService>();
        services.AddSingleton<IPluginLifecycleService, PluginLifecycleService>();
        services.AddSingleton<IPluginDiscoverer, ManifestPluginDiscoverer>();
        services.AddSingleton<IPluginDependencyResolver, PluginDependencyResolver>();

        // Host tool registry. New tool types are wired up here once and then resolved per plugin id.
        services.AddSingleton<IPluginToolRegistry>(sp =>
        {
            var registry = new PluginToolRegistry();

            // Each plugin gets its own encryption key.
            registry.RegisterFactory<IPluginEncryptionTool>(
                pluginId => new AesGcmPluginEncryptionTool(pluginId));

            // Storage is shared at the database level but isolated by plugin scope.
            var storage = sp.GetRequiredService<IStorageService>();
            registry.RegisterFactory<IPluginStorageTool>(
                pluginId => new ScopedPluginStorageTool(pluginId, storage));

            return registry;
        });

        services.AddSingleton<IPluginLoader, PluginLoader>();

        return services;
    }
}
