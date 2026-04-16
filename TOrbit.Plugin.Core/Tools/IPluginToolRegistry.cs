namespace TOrbit.Plugin.Core.Tools;

/// <summary>
/// 宿主工具注册中心。按插件 ID 分发工具实例（单插件单实例，懒创建）。
/// 在 DI 中注册为 Singleton，由 <see cref="PluginContext.GetTool{T}"/> 内部使用。
/// </summary>
public interface IPluginToolRegistry
{
    /// <summary>
    /// 注册一种能力类型的工厂函数。应在应用启动、插件加载之前调用。
    /// 隔离保证来自注册行为本身：只有显式注册的类型才能被插件获取。
    /// </summary>
    void RegisterFactory<T>(Func<string, T> factory) where T : class;

    /// <summary>
    /// 获取指定插件的能力实例。若类型未注册则返回 null。
    /// </summary>
    T? GetTool<T>(string pluginId) where T : class;
}
