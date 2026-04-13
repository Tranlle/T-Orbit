using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Core.Services;

namespace Tranbok.Tools.Core.DependencyInjection;

public static class ToolHostCoreServiceCollectionExtensions
{
    public static IServiceCollection AddToolHostCore(this IServiceCollection services)
    {
        services.AddSingleton<IAppShellService, AppShellService>();
        services.AddSingleton<IPluginCatalogService, PluginCatalogService>();
        services.AddSingleton<IPluginDiscoveryService, PluginDiscoveryService>();
        return services;
    }
}
