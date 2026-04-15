using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public sealed class KeyMapService : IKeyMapService
{
    private readonly IStorageService _storage;
    private readonly List<KeyMapEntry> _entries = [];

    public KeyMapService(IStorageService storage) => _storage = storage;

    public IReadOnlyList<KeyMapEntry> Entries => _entries;

    public void Register(
        string id,
        string pluginId,
        string pluginName,
        string name,
        string description,
        string defaultKey,
        Action handler)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == id);
        if (existing is not null)
            _entries.Remove(existing);

        _entries.Add(new KeyMapEntry
        {
            Id          = id,
            PluginId    = pluginId,
            PluginName  = pluginName,
            Name        = name,
            Description = description,
            DefaultKey  = defaultKey,
            Handler     = handler,
            IsEnabled   = existing?.IsEnabled ?? true,
            CustomKey   = existing?.CustomKey
        });
    }

    public bool Dispatch(string keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString))
            return false;

        foreach (var entry in _entries)
        {
            if (!entry.IsEnabled || entry.Handler is null)
                continue;

            if (string.Equals(entry.EffectiveKey, keyString, StringComparison.OrdinalIgnoreCase))
            {
                entry.Handler.Invoke();
                return true;
            }
        }

        return false;
    }

    public void Load()
    {
        var stored = _storage.LoadKeyMapBindings();
        foreach (var row in stored)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == row.Id);
            if (entry is null) continue;
            entry.CustomKey = row.CustomKey;
            entry.IsEnabled = row.IsEnabled;
        }
    }

    public void Save()
    {
        _storage.SaveKeyMapBindings(_entries.Select(e => new KeyMapStoreEntry
        {
            Id        = e.Id,
            CustomKey = e.CustomKey,
            IsEnabled = e.IsEnabled
        }));
    }

    public void Reset(string? id = null)
    {
        if (id is null)
        {
            foreach (var entry in _entries)
            {
                entry.CustomKey = null;
                entry.IsEnabled = true;
            }
        }
        else
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry is null) return;
            entry.CustomKey = null;
            entry.IsEnabled = true;
        }
    }
}
