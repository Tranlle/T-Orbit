using Tranbok.Tools.Plugin.Core.Enums;

namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginDescriptor(
    string Id,
    string Name,
    string Version,
    string EntryAssembly,
    string EntryType,
    string? Description = null,
    string? Author = null,
    string? Icon = null,
    PluginLoadMode LoadMode = PluginLoadMode.Lazy,
    PluginIsolationMode IsolationMode = PluginIsolationMode.AssemblyLoadContext);
