using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Core.Tools;
using Tranbok.Tools.Plugin.Core.Tools;

namespace Tranbok.Tools.Core.DependencyInjection;

public static class ToolHostCoreServiceCollectionExtensions
{
    public static IServiceCollection AddToolHostCore(this IServiceCollection services)
    {
        // ── 基础设施 ────────────────────────────────────────────────────────
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IAppShellService, AppShellService>();
        services.AddSingleton<IAppPreferencesService, AppPreferencesService>();
        services.AddSingleton<IPluginCatalogService, PluginCatalogService>();
        services.AddSingleton<IPluginDiscoveryService, PluginDiscoveryService>();
        services.AddSingleton<IPluginVariableService, PluginVariableService>();
        services.AddSingleton<IKeyMapService, KeyMapService>();

        // ── Tool 注册中心（工厂注册，新增 Tool 只改这里）──────────────────
        services.AddSingleton<IPluginToolRegistry>(sp =>
        {
            var registry = new PluginToolRegistry();

            // 加密 Tool：每个插件独立密钥实例
            registry.RegisterFactory<IPluginEncryptionTool>(
                pluginId => new AesGcmPluginEncryptionTool(pluginId));

            // 存储 Tool：共享 DB 连接，按 pluginId 限定 kv: 作用域
            var storage = sp.GetRequiredService<IStorageService>();
            registry.RegisterFactory<IPluginStorageTool>(
                pluginId => new ScopedPluginStorageTool(pluginId, storage));

            return registry;
        });

        return services;
    }
}
