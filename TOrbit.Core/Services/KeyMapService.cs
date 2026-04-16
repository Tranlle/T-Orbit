using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

public sealed class KeyMapService : IKeyMapService
{
    private readonly IStorageService _storage;
    private readonly List<KeyMapEntry> _entries = [];

    // Dispatch index: EffectiveKey (case-insensitive) -> enabled key binding.
    // Rebuilt after Register, Load, Reset and Save.
    private readonly Dictionary<string, KeyMapEntry> _dispatchIndex =
        new(StringComparer.OrdinalIgnoreCase);

    public KeyMapService(IStorageService storage) => _storage = storage;

    public IReadOnlyList<KeyMapEntry> Entries => _entries;
    public event EventHandler? Changed;

    public void Register(
        string id,
        string pluginId,
        string pluginName,
        string name,
        string description,
        string defaultKey,
        Action handler)
    {
        var existing = _entries.FirstOrDefault(entry => entry.Id == id);
        if (existing is not null)
            _entries.Remove(existing);

        _entries.Add(new KeyMapEntry
        {
            Id = id,
            PluginId = pluginId,
            PluginName = pluginName,
            Name = name,
            Description = description,
            DefaultKey = defaultKey,
            Handler = handler,
            IsEnabled = existing?.IsEnabled ?? true,
            CustomKey = existing?.CustomKey
        });

        RebuildIndex();
        NotifyChanged();
    }

    public bool Dispatch(string keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString))
            return false;

        if (!_dispatchIndex.TryGetValue(keyString, out var entry))
            return false;

        entry.Handler?.Invoke();
        return true;
    }

    public void Load()
    {
        var stored = _storage.LoadKeyMapBindings();
        foreach (var row in stored)
        {
            var entry = _entries.FirstOrDefault(x => x.Id == row.Id);
            if (entry is null)
                continue;

            entry.CustomKey = row.CustomKey;
            entry.IsEnabled = row.IsEnabled;
        }

        RebuildIndex();
        NotifyChanged();
    }

    public void Save()
    {
        _storage.SaveKeyMapBindings(_entries.Select(entry => new KeyMapStoreEntry
        {
            Id = entry.Id,
            CustomKey = entry.CustomKey,
            IsEnabled = entry.IsEnabled
        }));

        RebuildIndex();
        NotifyChanged();
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
            var entry = _entries.FirstOrDefault(x => x.Id == id);
            if (entry is null)
                return;

            entry.CustomKey = null;
            entry.IsEnabled = true;
        }

        RebuildIndex();
        NotifyChanged();
    }

    private void RebuildIndex()
    {
        _dispatchIndex.Clear();
        foreach (var entry in _entries)
        {
            if (!entry.IsEnabled || entry.Handler is null)
                continue;

            // When multiple bindings use the same key, the earliest registered binding wins.
            _dispatchIndex.TryAdd(entry.EffectiveKey, entry);
        }
    }

    private void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
