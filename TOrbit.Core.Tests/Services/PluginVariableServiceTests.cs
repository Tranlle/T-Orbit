using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Core.Tools;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class PluginVariableServiceTests
{
    [Fact]
    public void Save_EncryptsSensitiveValuesBeforePersisting()
    {
        var catalog = new PluginCatalogService();
        var plugin = new TestVariablePlugin();
        catalog.Register(plugin);

        var storage = new InMemoryStorageService();
        var registry = new TestToolRegistry();
        registry.RegisterEncryptionTool(plugin.Descriptor.Id, new TestEncryptionTool());

        var service = new PluginVariableService(catalog, registry, storage);
        service.Save(new PluginVariableStore
        {
            Entries =
            [
                new PluginVariableEntry
                {
                    PluginId = plugin.Descriptor.Id,
                    Key = "TEST_SECRET",
                    Value = "plain-secret",
                    IsEncrypted = true
                }
            ]
        });

        var stored = Assert.Single(storage.GetAllKvWithMeta(PluginVariableService.ScopeFor(plugin.Descriptor.Id)));
        Assert.Equal("enc:plain-secret", stored.Value);
        Assert.True(stored.IsEncrypted);
    }

    [Fact]
    public void InjectOne_PushesRawPersistedValuesWithDefaults()
    {
        var catalog = new PluginCatalogService();
        var plugin = new TestVariablePlugin();
        catalog.Register(plugin);

        var storage = new InMemoryStorageService();
        storage.SetKvWithMeta(PluginVariableService.ScopeFor(plugin.Descriptor.Id), "TEST_SECRET", "enc:plain-secret", true);

        var registry = new TestToolRegistry();
        registry.RegisterEncryptionTool(plugin.Descriptor.Id, new TestEncryptionTool());

        var service = new PluginVariableService(catalog, registry, storage);
        service.InjectOne(plugin);

        Assert.Equal("enc:plain-secret", plugin.ReceivedValues["TEST_SECRET"]);
        Assert.Equal("default-visible", plugin.ReceivedValues["TEST_VISIBLE"]);
    }

    [Fact]
    public void GetValue_DecryptsEncryptedRows()
    {
        var catalog = new PluginCatalogService();
        var plugin = new TestVariablePlugin();
        catalog.Register(plugin);

        var storage = new InMemoryStorageService();
        storage.SetKvWithMeta(PluginVariableService.ScopeFor(plugin.Descriptor.Id), "TEST_SECRET", "enc:plain-secret", true);

        var registry = new TestToolRegistry();
        registry.RegisterEncryptionTool(plugin.Descriptor.Id, new TestEncryptionTool());

        var service = new PluginVariableService(catalog, registry, storage);
        var value = service.GetValue(plugin.Descriptor.Id, "TEST_SECRET");

        Assert.Equal("plain-secret", value);
    }

    private sealed class TestVariablePlugin : BasePlugin, IPluginVariableReceiver
    {
        public override PluginDescriptor Descriptor { get; } = new(
            "torbit.test.variables",
            "Variable Test",
            "1.0.0",
            "VariableTest.dll",
            "VariableTest.Plugin",
            VariableDefinitions:
            [
                new PluginVariableDefinition("TEST_SECRET", "", IsEncrypted: true),
                new PluginVariableDefinition("TEST_VISIBLE", "default-visible")
            ],
            Kind: PluginKind.Service);

        public IReadOnlyDictionary<string, string> ReceivedValues { get; private set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void OnVariablesInjected(IReadOnlyDictionary<string, string> values)
        {
            ReceivedValues = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        }
    }

    private sealed class TestEncryptionTool : IPluginEncryptionTool
    {
        public string Encrypt(string plaintext) => $"enc:{plaintext}";

        public string? TryDecrypt(string ciphertext)
            => ciphertext.StartsWith("enc:", StringComparison.Ordinal) ? ciphertext[4..] : null;
    }

    private sealed class TestToolRegistry : IPluginToolRegistry
    {
        private readonly Dictionary<string, object> _tools = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterFactory<T>(Func<string, T> factory) where T : class
            => throw new NotSupportedException();

        public void RegisterEncryptionTool(string pluginId, IPluginEncryptionTool tool)
        {
            _tools[$"{pluginId}:{typeof(IPluginEncryptionTool).FullName}"] = tool;
        }

        public T? GetTool<T>(string pluginId) where T : class
            => _tools.TryGetValue($"{pluginId}:{typeof(T).FullName}", out var tool) ? tool as T : null;
    }

    private sealed class InMemoryStorageService : IStorageService
    {
        private readonly Dictionary<string, Dictionary<string, KvEntry>> _scopes = new(StringComparer.OrdinalIgnoreCase);

        public string? GetKv(string scope, string key) => GetKvWithMeta(scope, key)?.Value;

        public void SetKv(string scope, string key, string? value) => SetKvWithMeta(scope, key, value, false);

        public void DeleteKv(string scope, string key)
        {
            if (_scopes.TryGetValue(scope, out var items))
                items.Remove(key);
        }

        public IReadOnlyDictionary<string, string?> GetAllKv(string scope)
            => GetScope(scope).ToDictionary(item => item.Key, item => item.Value.Value, StringComparer.OrdinalIgnoreCase);

        public void SetKvWithMeta(string scope, string key, string? value, bool isEncrypted)
        {
            GetScope(scope)[key] = new KvEntry(key, value, isEncrypted);
        }

        public void SetKvBatch(IEnumerable<(string scope, string key, string? value, bool isEncrypted)> entries)
        {
            foreach (var entry in entries)
                SetKvWithMeta(entry.scope, entry.key, entry.value, entry.isEncrypted);
        }

        public KvEntry? GetKvWithMeta(string scope, string key)
            => GetScope(scope).TryGetValue(key, out var entry) ? entry : null;

        public IReadOnlyList<KvEntry> GetAllKvWithMeta(string scope) => GetScope(scope).Values.ToList();

        public IReadOnlyDictionary<string, IReadOnlyList<KvEntry>> GetAllKvWithMetaByPrefix(string scopePrefix)
            => _scopes
                .Where(item => item.Key.StartsWith(scopePrefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    item => item.Key,
                    item => (IReadOnlyList<KvEntry>)item.Value.Values.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<KeyMapStoreEntry> LoadKeyMapBindings() => [];

        public void SaveKeyMapBindings(IEnumerable<KeyMapStoreEntry> entries)
        {
        }

        public Task<string?> GetAsync(string scope, string key) => Task.FromResult(GetKv(scope, key));

        public Task SetAsync(string scope, string key, string? value)
        {
            SetKv(scope, key, value);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string scope, string key)
        {
            DeleteKv(scope, key);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, string?>> GetAllAsync(string scope)
            => Task.FromResult(GetAllKv(scope));

        private Dictionary<string, KvEntry> GetScope(string scope)
        {
            if (!_scopes.TryGetValue(scope, out var entries))
            {
                entries = new Dictionary<string, KvEntry>(StringComparer.OrdinalIgnoreCase);
                _scopes[scope] = entries;
            }

            return entries;
        }
    }
}
