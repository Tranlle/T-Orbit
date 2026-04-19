namespace TOrbit.Plugin.RuntimeHost.Models;

public sealed class RuntimeStartRequest
{
    public string ProfileId { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public string EntryRelativePath { get; init; } = string.Empty;

    public bool RunWithDotnet { get; init; }

    public IReadOnlyList<HostedAppEnvironmentVariable> EnvironmentVariables { get; init; } = [];
}
