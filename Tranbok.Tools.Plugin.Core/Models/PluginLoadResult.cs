namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginLoadResult(
    PluginHandle Handle,
    PluginCompatibilityResult Compatibility,
    IReadOnlyCollection<string> Warnings);
