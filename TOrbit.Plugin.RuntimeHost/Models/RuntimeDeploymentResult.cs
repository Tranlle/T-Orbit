namespace TOrbit.Plugin.RuntimeHost.Models;

public sealed class RuntimeDeploymentResult
{
    public string PackagePath { get; init; } = string.Empty;

    public string EntryRelativePath { get; init; } = string.Empty;

    public bool RunWithDotnet { get; init; }

    public string PackageHash { get; init; } = string.Empty;

    public DateTimeOffset DeployedAt { get; init; }
}
