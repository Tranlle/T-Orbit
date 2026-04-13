namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginDependencyGraph(
    IReadOnlyDictionary<string, IReadOnlyCollection<PluginDependency>> Dependencies,
    IReadOnlyCollection<string> LoadOrder);
