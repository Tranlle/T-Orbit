namespace Tranbok.Tools.Plugin.Core.Tools;

/// <summary>
/// 宿主提供的 KV 存储工具。每个插件获得一个绑定了自身 ID 的 scoped 实例，
/// 只能读写本插件作用域下的数据，无法访问其他插件或系统作用域。
/// <para>
/// 插件如需持久化加密数据，应先通过 <see cref="IPluginEncryptionTool"/> 加密再写入。
/// </para>
/// </summary>
public interface IPluginStorageTool : IPluginTool
{
    /// <summary>读取当前插件作用域下的键值，不存在时返回 null。</summary>
    Task<string?> GetAsync(string key);

    /// <summary>写入/更新键值。<paramref name="value"/> 为 null 时等同于删除。</summary>
    Task SetAsync(string key, string? value);

    /// <summary>删除指定键，键不存在时无操作。</summary>
    Task DeleteAsync(string key);

    /// <summary>获取当前插件作用域下的全部键值对。</summary>
    Task<IReadOnlyDictionary<string, string?>> GetAllAsync();
}
