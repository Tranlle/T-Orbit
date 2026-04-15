using System.Collections.Concurrent;
using Tranbok.Tools.Plugin.Core.Tools;

namespace Tranbok.Tools.Core.Tools;

/// <summary>
/// 宿主工具注册中心实现（Singleton）。
/// <para>
/// 采用工厂注册模式：新增 Tool 类型只需在 DI 配置时调用 <see cref="RegisterFactory{T}"/>，
/// 无需修改本类。每个插件的工具实例按 pluginId 懒创建并缓存。
/// </para>
/// </summary>
public sealed class PluginToolRegistry : IPluginToolRegistry
{
    // 工厂：toolType → (pluginId → IPluginTool)
    private readonly Dictionary<Type, Func<string, IPluginTool>> _factories = [];

    // 缓存：(pluginId, toolType) → IPluginTool
    private readonly ConcurrentDictionary<(string, Type), IPluginTool> _cache = new();

    /// <summary>
    /// 注册一种 Tool 类型的工厂函数（应在应用启动时调用）。
    /// </summary>
    public void RegisterFactory<T>(Func<string, T> factory) where T : class, IPluginTool
        => _factories[typeof(T)] = pluginId => factory(pluginId);

    /// <inheritdoc/>
    public T? GetTool<T>(string pluginId) where T : class, IPluginTool
    {
        if (!_factories.TryGetValue(typeof(T), out var factory))
            return null;

        return _cache.GetOrAdd((pluginId, typeof(T)), _ => factory(pluginId)) as T;
    }
}
