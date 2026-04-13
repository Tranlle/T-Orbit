namespace Tranbok.Tools.Core.Models;

public sealed record LoadedPluginDescriptor(
    PluginEntry Entry,
    string AssemblyPath,
    string PluginDirectory);
