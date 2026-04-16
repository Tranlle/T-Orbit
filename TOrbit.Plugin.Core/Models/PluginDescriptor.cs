using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Core;

public sealed record PluginDescriptor(
    string Id,
    string Name,
    string Version,
    string EntryAssembly,
    string EntryType,
    string? Description = null,
    string? Author = null,
    string? Icon = null,
    string? Tags = null,
    PluginLoadMode LoadMode = PluginLoadMode.Lazy,
    PluginIsolationMode IsolationMode = PluginIsolationMode.AssemblyLoadContext,
    IReadOnlyList<PluginVariableDefinition>? VariableDefinitions = null,
    PluginKind Kind = PluginKind.Visual,
    IReadOnlyList<PluginCapability>? Capabilities = null);
