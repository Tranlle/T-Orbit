using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Plugin.Core.Tools;

namespace Tranbok.Tools.Core.Tools;

/// <summary>
/// <see cref="IPluginStorageTool"/> 的 scoped 包装实现。
/// 构造时绑定 <paramref name="pluginId"/>，所有操作都限定在 <c>"kv:{pluginId}"</c> 作用域，
/// 插件无法越界访问其他作用域。
/// </summary>
internal sealed class ScopedPluginStorageTool : IPluginStorageTool
{
    private readonly string _scope;
    private readonly IStorageService _storage;

    internal ScopedPluginStorageTool(string pluginId, IStorageService storage)
    {
        _scope   = $"kv:{pluginId}";
        _storage = storage;
    }

    public Task<string?> GetAsync(string key)
        => _storage.GetAsync(_scope, key);

    public Task SetAsync(string key, string? value)
        => _storage.SetAsync(_scope, key, value);

    public Task DeleteAsync(string key)
        => _storage.DeleteAsync(_scope, key);

    public Task<IReadOnlyDictionary<string, string?>> GetAllAsync()
        => _storage.GetAllAsync(_scope);
}
