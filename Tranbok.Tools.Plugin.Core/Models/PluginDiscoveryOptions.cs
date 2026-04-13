namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginDiscoveryOptions(
    string RootDirectory,
    string SearchPattern = "*",
    bool Recursive = true);
