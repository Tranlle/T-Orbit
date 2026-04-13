namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginDependency(
    string PluginId,
    string VersionRange,
    bool IsOptional = false);
