using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Tools;

namespace TOrbit.Plugin.Core;

public sealed record PluginContext(
    string PluginId,
    string BaseDirectory,
    IPluginToolRegistry ToolRegistry,
    HostEnvironmentInfo HostEnvironment,
    PluginIsolationMode IsolationMode,
    IReadOnlyDictionary<string, object?> Properties)
{
    /// <summary>
    /// 从宿主工具注册中心获取当前插件的工具实例。
    /// 工具按插件 ID 隔离（如 <see cref="IPluginEncryptionTool"/> 的密钥独立存储）。
    /// 若工具类型未注册，返回 null。
    /// </summary>
    public T? GetTool<T>() where T : class
        => ToolRegistry.GetTool<T>(PluginId);
}
