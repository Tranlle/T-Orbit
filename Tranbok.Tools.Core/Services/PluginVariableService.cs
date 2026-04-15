using Tranbok.Tools.Core.Models;
using Tranbok.Tools.Plugin.Core.Abstractions;
using Tranbok.Tools.Plugin.Core.Tools;

namespace Tranbok.Tools.Core.Services;

public sealed class PluginVariableService : IPluginVariableService
{
    // 作用域前缀，供 StorageService 迁移时引用
    internal static string ScopeFor(string pluginId) => $"vars:{pluginId}";

    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginToolRegistry   _toolRegistry;
    private readonly IStorageService       _storage;

    public PluginVariableService(
        IPluginCatalogService pluginCatalog,
        IPluginToolRegistry   toolRegistry,
        IStorageService       storage)
    {
        _pluginCatalog = pluginCatalog;
        _toolRegistry  = toolRegistry;
        _storage       = storage;
    }

    // ── Load（构建 PluginVariableStore 供 Settings UI 使用）──────────────────

    public PluginVariableStore Load()
    {
        var store = new PluginVariableStore();

        foreach (var plugin in _pluginCatalog.Plugins)
        {
            var defs = plugin.Plugin.Descriptor.VariableDefinitions;
            if (defs is null || defs.Count == 0) continue;

            var scope   = ScopeFor(plugin.Id);
            var dbRows  = _storage.GetAllKvWithMeta(scope)
                .ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var def in defs)
            {
                if (dbRows.TryGetValue(def.Key, out var row))
                {
                    store.Entries.Add(new PluginVariableEntry
                    {
                        PluginId    = plugin.Id,
                        Key         = def.Key,
                        Value       = row.Value ?? string.Empty,
                        IsEncrypted = row.IsEncrypted
                    });
                }
                // 未存储的条目不写入 store，Save 时按 defs 驱动写入
            }
        }

        return store;
    }

    // ── Save（明文传入，IsEncrypted=true 的条目由基座 Tool 加密后写库）────────

    public void Save(PluginVariableStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        foreach (var entry in store.Entries)
        {
            var value = entry.Value;
            if (entry.IsEncrypted && !string.IsNullOrEmpty(value))
            {
                var tool = _toolRegistry.GetTool<IPluginEncryptionTool>(entry.PluginId);
                if (tool is not null)
                    value = tool.Encrypt(value);
            }

            _storage.SetKvWithMeta(
                ScopeFor(entry.PluginId),
                entry.Key,
                value,
                entry.IsEncrypted);
        }
    }

    // ── GetValue（Settings UI 用，自动解密加密字段）───────────────────────────

    public string? GetValue(string pluginId, string key)
    {
        var scope = ScopeFor(pluginId);
        var rows  = _storage.GetAllKvWithMeta(scope);
        var row   = rows.FirstOrDefault(r =>
            string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

        if (row is not null)
        {
            if (row.IsEncrypted && !string.IsNullOrEmpty(row.Value))
            {
                var tool = _toolRegistry.GetTool<IPluginEncryptionTool>(pluginId);
                return tool?.TryDecrypt(row.Value) ?? string.Empty;
            }
            return row.Value;
        }

        return GetDefaultValue(pluginId, key);
    }

    // ── InjectAll（将原始存储值推送给插件，由插件自行解密）──────────────────────

    public void InjectAll()
    {
        foreach (var entry in _pluginCatalog.Plugins)
            InjectToPlugin(entry.Plugin);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void InjectToPlugin(IPlugin plugin)
    {
        if (plugin is not IPluginVariableReceiver receiver)
            return;

        var definitions = plugin.Descriptor.VariableDefinitions;
        if (definitions is null || definitions.Count == 0)
            return;

        var scope  = ScopeFor(plugin.Descriptor.Id);
        var dbDict = _storage.GetAllKvWithMeta(scope)
            .ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in definitions)
        {
            // 传原始值：有存储值则传存储值（可能是密文），否则传默认值（明文）
            dict[def.Key] = dbDict.TryGetValue(def.Key, out var val)
                ? val ?? string.Empty
                : def.DefaultValue;
        }

        receiver.OnVariablesInjected(dict);
    }

    private string? GetDefaultValue(string pluginId, string key)
    {
        var plugin = _pluginCatalog.Plugins.FirstOrDefault(p =>
            string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        return plugin?.Plugin.Descriptor.VariableDefinitions?
            .FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.DefaultValue;
    }
}
