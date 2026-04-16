namespace TOrbit.Core.Models;

public sealed record PluginValidationState(
    string PluginId,
    string PluginName,
    IReadOnlyList<string> Messages)
{
    public bool HasIssues => Messages.Count > 0;
}
