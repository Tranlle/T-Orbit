using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

public interface IPluginValidationStatusService
{
    event EventHandler<string>? ValidationChanged;

    PluginValidationState Get(string pluginId, string pluginName);

    void Set(string pluginId, string pluginName, IReadOnlyList<string> messages);

    void Clear(string pluginId, string pluginName);
}
