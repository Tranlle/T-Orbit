using Tranbok.Tools.Plugin.Core.Enums;

namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginContext(
    string PluginId,
    string BaseDirectory,
    IServiceProvider? Services,
    HostEnvironmentInfo HostEnvironment,
    PluginIsolationMode IsolationMode,
    IReadOnlyDictionary<string, object?> Properties);
