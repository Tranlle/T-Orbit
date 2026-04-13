namespace Tranbok.Tools.Core.Models;

public sealed record PluginLoadError(
    string AssemblyPath,
    string Message,
    string? PluginId = null,
    Exception? Exception = null);
