using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

public sealed class PluginValidationStatusService : IPluginValidationStatusService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, PluginValidationState> _states = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<string>? ValidationChanged;

    public PluginValidationState Get(string pluginId, string pluginName)
    {
        lock (_syncRoot)
        {
            return _states.TryGetValue(pluginId, out var state)
                ? state
                : new PluginValidationState(pluginId, pluginName, []);
        }
    }

    public void Set(string pluginId, string pluginName, IReadOnlyList<string> messages)
    {
        var state = new PluginValidationState(pluginId, pluginName, messages);

        lock (_syncRoot)
        {
            if (messages.Count == 0)
                _states.Remove(pluginId);
            else
                _states[pluginId] = state;
        }

        ValidationChanged?.Invoke(this, pluginId);
    }

    public void Clear(string pluginId, string pluginName)
        => Set(pluginId, pluginName, []);
}
