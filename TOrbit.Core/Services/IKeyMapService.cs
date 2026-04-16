using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

public interface IKeyMapService
{
    IReadOnlyList<KeyMapEntry> Entries { get; }

    event EventHandler? Changed;

    void Register(
        string id,
        string pluginId,
        string pluginName,
        string name,
        string description,
        string defaultKey,
        Action handler);

    bool Dispatch(string keyString);

    void Load();

    void Save();

    void Reset(string? id = null);
}
